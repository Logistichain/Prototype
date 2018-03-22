using Mpb.Consensus.Logic.Constants;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class DifficultyCalculator
    {
        public BigDecimal CalculateCurrentDifficulty(Blockchain chain)
        {
            return CalculateDifficultyForHeight(chain, chain.CurrentHeight);
        }
        public BigDecimal CalculateDifficultyForHeight(Blockchain chain, int height)
        {
            return CalculateDifficulty(chain, height, BlockchainConstants.ProtocolVersion, BlockchainConstants.SecondsPerBlockGoal, BlockchainConstants.DifficultyUpdateCycle);
        }

        public BigDecimal CalculateDifficulty(Blockchain chain, int height, int protocolVersion, int secondsPerBlockGoal, int difficultyUpdateCycle)
        {
            if (height < difficultyUpdateCycle)
            {
                return 1;
            }

            var previousDifficultyInfo = GetPreviousDifficultyUpdateInformation(height, chain, difficultyUpdateCycle);            
            var totalSeconds = previousDifficultyInfo.TotalSecondsForBlocks == 0 ? 1 : previousDifficultyInfo.TotalSecondsForBlocks;
            BigDecimal difficultyMultiplier = (double)(secondsPerBlockGoal * difficultyUpdateCycle) / totalSeconds;
            var previousDifficulty = CalculateDifficulty(chain, previousDifficultyInfo.BeginHeight, protocolVersion, secondsPerBlockGoal, difficultyUpdateCycle); // This is highly inefficient, better to keep a record

            return previousDifficulty * difficultyMultiplier;
        }


        public BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain) { return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, BlockchainConstants.DifficultyUpdateCycle); }

        public BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain, int difficultyUpdateCycle) { return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, difficultyUpdateCycle); }

        public BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(int height, Blockchain chain, int difficultyUpdateCycle)
        {
            if (height < difficultyUpdateCycle)
            {
                throw new DifficultyCalculationException("Unable to calculate the previous difficulty because the height is lower than the DifficultyUpdateCycle.");
            }

            // The difficulty is calculated every n'th block.
            // If the given height is 2n+1, we need to calculate the difficulty for block 2n
            int calculateForHeight = height - height % difficultyUpdateCycle;
            long firstBlockStarted = chain.Blocks[calculateForHeight - difficultyUpdateCycle].Timestamp;
            long lastBlockStarted = chain.Blocks[calculateForHeight].Timestamp; // Take care, this is the time when the miner STARTED mining this block
            long totalSecondsNeeded = lastBlockStarted - firstBlockStarted;

            return new BlockDifficultyUpdate(chain, calculateForHeight - difficultyUpdateCycle, calculateForHeight);
        }
    }
}
