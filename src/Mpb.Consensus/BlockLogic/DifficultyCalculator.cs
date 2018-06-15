using Mpb.Consensus.Exceptions;
using Mpb.Model;
using Mpb.Shared;
using Mpb.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.BlockLogic
{
    public class DifficultyCalculator : IDifficultyCalculator
    {
        private Dictionary<int, BigDecimal> calculatedDifficulties;

        public DifficultyCalculator()
        {
            // todo load (and save) previously calculated difficulties from/to DAL
            calculatedDifficulties = new Dictionary<int, BigDecimal>();
        }

        public virtual BigDecimal CalculateCurrentDifficulty(Blockchain chain)
        {
            return CalculateDifficultyForHeight(chain, chain.CurrentHeight);
        }

        public virtual BigDecimal CalculateDifficultyForHeight(Blockchain chain, int height)
        {
            return CalculateDifficulty(chain, height, BlockchainConstants.ProtocolVersion, BlockchainConstants.SecondsPerBlockGoal, BlockchainConstants.DifficultyUpdateCycle);
        }

        public virtual BigDecimal CalculateDifficulty(Blockchain chain, int height, uint protocolVersion, uint secondsPerBlockGoal, int difficultyUpdateCycle)
        {
            if (height < difficultyUpdateCycle)
            {
                return 1;
            }

            if (calculatedDifficulties.TryGetValue(height, out BigDecimal cachedDifficulty))
            {
                return cachedDifficulty;
            }

            lock (chain)
            {
                var previousDifficultyInfo = GetPreviousDifficultyUpdateInformation(height, chain, difficultyUpdateCycle);            
                var totalSeconds = previousDifficultyInfo.TotalSecondsForBlocks == 0 ? 1 : previousDifficultyInfo.TotalSecondsForBlocks;
                BigDecimal difficultyMultiplier = (double)(secondsPerBlockGoal * difficultyUpdateCycle) / totalSeconds;
                BigDecimal previousDifficulty = 1;
                if (!calculatedDifficulties.TryGetValue(previousDifficultyInfo.BeginHeight, out previousDifficulty))
                {
                    previousDifficulty = CalculateDifficulty(chain, previousDifficultyInfo.BeginHeight, protocolVersion, secondsPerBlockGoal, difficultyUpdateCycle); // This is highly inefficient, better to keep a record
                }
                // todo round the number somehow as it gets larger every time.

                var currentDificulty = previousDifficulty * difficultyMultiplier;
                calculatedDifficulties.Add(height, currentDificulty);
                return currentDificulty;
            }
        }

        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain) {
            return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, BlockchainConstants.DifficultyUpdateCycle);
        }

        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain, int difficultyUpdateCycle) {
            return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, difficultyUpdateCycle);
        }

        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(int height, Blockchain chain, int difficultyUpdateCycle)
        {
            // todo throw on difficultyUpdateCycle negative value and test it

            // The difficulty is calculated every n'th block.
            // If the given height is 2n+3, we need to calculate the difficulty for block 2n
            int calculateForHeight = height - height % difficultyUpdateCycle;

            if (calculateForHeight < difficultyUpdateCycle)
            {
                throw new DifficultyCalculationException("Unable to calculate the previous difficulty because the height is lower than the DifficultyUpdateCycle.");
            }

            // - 1 because difficultyUpdate is based on counts, as where calculateForHeight is based on index.
            //! Take care, the lastBlockStarted this is the time when the miner STARTED mining this block
            lock (chain)
            {
                long firstBlockStarted = chain.Blocks[calculateForHeight - difficultyUpdateCycle].Header.Timestamp;
                long lastBlockStarted = chain.Blocks[calculateForHeight - 1].Header.Timestamp;

                return new BlockDifficultyUpdate(chain, calculateForHeight - difficultyUpdateCycle, calculateForHeight - 1);
            }
        }


    }
}
