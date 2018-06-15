using Logistichain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Consensus.Exceptions
{
    /// <summary>
    /// Throw this exception when the given block doesn't pass validation.
    /// </summary>
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
