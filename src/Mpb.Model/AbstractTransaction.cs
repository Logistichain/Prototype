using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public abstract class AbstractTransaction
    {
        private readonly uint _version;
        private readonly string _action;
        private readonly string _data;
        private readonly uint _fee;
        private string _hash;
        private string _signature;
        public uint Version => _version;
        public string Action => _action;
        public string Data => _data;
        public uint Fee => _fee;

        /// <summary>
        /// The string representation of the transaction hash.
        /// Does not contain dashes (-).
        /// </summary>
        public string Hash => _hash;
        /// <summary>
        /// The hash value, encrypted with the sender's private key
        /// </summary>
        public string Signature => _signature;

        protected AbstractTransaction(uint version, string action, string data, uint fee)
        {
            _version = version;
            _action = action;
            _data = data;
            _fee = fee;
        }

        public bool IsFinalized() => _hash != null && _signature != null;

        public void Finalize(string hash, string signature)
        {
            if (!IsFinalized())
            {
                _hash = hash;
                _signature = signature;
            }
            else
            {
                throw new InvalidOperationException("Transaction is already finalized");
            }
        }
    }
}
