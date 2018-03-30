using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Logic.Exceptions
{
    public class BlockRejectedException : Exception
    {
        public BlockRejectedException()
        {
        }

        public BlockRejectedException(string message) : base(message)
        {
        }

        public BlockRejectedException(string message, Block b) : base(message)
        {
            Block = b;
        }

        public Block Block { get; }
    }
}
