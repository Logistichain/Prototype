using Mpb.Consensus.Logic.Constants;
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
            return CalculateDifficultyForHeight(chain, chain.Blocks.Count - 1);
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

            // The difficulty is calculated every n'th block.
            // If the given height is 2n+1, we need to calculate the difficulty for block 2n
            int calculateForHeight = height - height % difficultyUpdateCycle;
            long firstBlockStarted = chain.Blocks[calculateForHeight - difficultyUpdateCycle].Timestamp;
            long lastBlockStarted = chain.Blocks[calculateForHeight].Timestamp; // Take care, this is the time when the miner STARTED mining this block
            long totalSecondsNeeded = lastBlockStarted - firstBlockStarted;

            // Because we don't know how much time it took to mine the n'th block, we need to calculate the total time for n-1 blocks
            BigDecimal difficultyMultiplier = ((secondsPerBlockGoal * difficultyUpdateCycle) - secondsPerBlockGoal) / totalSecondsNeeded;
            var previousDifficulty = CalculateDifficulty(chain, height-1, protocolVersion, secondsPerBlockGoal, difficultyUpdateCycle); // This is highly inefficient, better to keep a record

            return previousDifficulty * difficultyMultiplier;
        }
    }
}
