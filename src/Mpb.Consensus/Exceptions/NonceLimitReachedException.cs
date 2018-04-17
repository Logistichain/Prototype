using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Exceptions
{
    public class NonceLimitReachedException : Exception
    {
        public NonceLimitReachedException() : base("Nonce limit reached") { }
    }
}
