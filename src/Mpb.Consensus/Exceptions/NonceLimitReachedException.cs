using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Exceptions
{
    /// <summary>
    /// Thrown when the nonce reaches ulong.MaxValue
    /// </summary>
    public class NonceLimitReachedException : Exception
    {
        public NonceLimitReachedException() : base("Nonce limit reached") { }
    }
}
