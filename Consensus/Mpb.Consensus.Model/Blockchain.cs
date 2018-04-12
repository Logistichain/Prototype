using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class Blockchain
    {
        private List<Block> _blocks;

        /// <summary>
        /// The blocks in this blockchain
        /// Height == index
        /// </summary>
        public List<Block> Blocks => _blocks;

        private string _net;

        /// <summary>
        /// Network identifier, like "mainnet" or "testnet"
        /// </summary>
        public string NetIdentifier => _net;

        /// <summary>
        /// The index of the last block that was added to this chain
        /// </summary>
        public virtual int CurrentHeight => _blocks.Count - 1;

        public Blockchain(List<Block> existingChain, string netIdentifier)
        {
            _blocks = existingChain;
            _net = netIdentifier;
        }

        public Blockchain(string netIdentifier) : this(new List<Block>(), netIdentifier) { }
    }
}
