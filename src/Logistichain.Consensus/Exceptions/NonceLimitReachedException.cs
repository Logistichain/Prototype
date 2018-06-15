using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Consensus.Exceptions
{
    /// <summary>
    /// Thrown when the nonce reaches ulong.MaxValue
    /// </summary>
    public class NonceLimitReachedException : Exception
    {
        public NonceLimitReachedException() : base("Nonce limit reached") { }
    }
}
