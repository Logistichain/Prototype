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
        public virtual BigDecimal CalculateCurrentDifficulty(Blockchain chain)
        {
            return CalculateDifficultyForHeight(chain, chain.CurrentHeight);
        }

        public virtual BigDecimal CalculateDifficultyForHeight(Blockchain chain, int height)
        {
            return CalculateDifficulty(chain, height, BlockchainConstants.ProtocolVersion, BlockchainConstants.SecondsPerBlockGoal, BlockchainConstants.DifficultyUpdateCycle);
        }

        public virtual BigDecimal CalculateDifficulty(Blockchain chain, int height, int protocolVersion, int secondsPerBlockGoal, int difficultyUpdateCycle)
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

        /// <summary>
        /// Get previous difficulty, following the consensus rules
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <returns>Information about the last block difficulty update</returns>
        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain) {
            return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, BlockchainConstants.DifficultyUpdateCycle);
        }

        /// <summary>
        /// Get the previous difficulty with a custom update cycle
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <param name="difficultyUpdateCycle">This describes that the difficulty is recalculated every x blocks</param>
        /// <returns>Information about the last block difficulty update</returns>
        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain, int difficultyUpdateCycle) {
            return GetPreviousDifficultyUpdateInformation(chain.CurrentHeight, chain, difficultyUpdateCycle);
        }

        public virtual BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(int height, Blockchain chain, int difficultyUpdateCycle)
        {
            // The difficulty is calculated every n'th block.
            // If the given height is 2n+3, we need to calculate the difficulty for block 2n
            // - 1 because difficultyUpdate is based on counts, as where calculateForHeight is based on index.
            int calculateForHeight = height - height % difficultyUpdateCycle;

            if (calculateForHeight < difficultyUpdateCycle)
            {
                throw new DifficultyCalculationException("Unable to calculate the previous difficulty because the height is lower than the DifficultyUpdateCycle.");
            }

            long firstBlockStarted = chain.Blocks[calculateForHeight - difficultyUpdateCycle].Timestamp;
            long lastBlockStarted = chain.Blocks[calculateForHeight-1].Timestamp; // Take care, this is the time when the miner STARTED mining this block

            return new BlockDifficultyUpdate(chain, calculateForHeight - difficultyUpdateCycle, calculateForHeight-1);
        }
    }
}
