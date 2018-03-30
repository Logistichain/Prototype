using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    /// <summary>
    /// This model is used to transfer information about a specific difficulty update to calculate the new difficulty.
    /// </summary>
    public class BlockDifficultyUpdate
    {
        private int _beginHeight;

        /// <summary>
        /// The start of the range of blocks
        /// </summary>
        public int BeginHeight => _beginHeight;

        private int _endHeight;

        /// <summary>
        /// The end of the range of blocks
        /// </summary>
        public int EndHeight => _endHeight;

        private Blockchain _blockchain;

        /// <summary>
        /// Use the given heights on this blockchain object
        /// </summary>
        public Blockchain Blockchain => _blockchain;

        /// <summary>
        /// The total time of seconds it took to create the blocks between BeginHeight and EndHeight
        /// Keep in mind that these block timestamps are the times when the miner STARTED mining,
        /// so the time it took to create the last block in this range (EndHeight) is not known.
        /// </summary>
        public long TotalSecondsForBlocks => _blockchain.Blocks[_endHeight].Timestamp - _blockchain.Blocks[_beginHeight].Timestamp;

        public BlockDifficultyUpdate(Blockchain chain, int beginHeight, int endHeight)
        {
            _blockchain = chain;
            _beginHeight = beginHeight;
            _endHeight = endHeight;
        }
    }
}
