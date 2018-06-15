using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Model
{
    public abstract class AbstractTransaction
    {
        private readonly uint _version;
        private readonly string _action;
        private readonly string _data;
        private readonly uint _fee;
        private string _hash;
        private string _signature;

        /// <summary>
        /// The transaction version
        /// </summary>
        public uint Version => _version;

        /// <summary>
        /// The action to perform
        /// <seealso cref="Shared.Constants.TransactionAction"/>
        /// </summary>
        public string Action => _action;

        /// <summary>
        /// Free text field
        /// (or in case of 'createsku' transaction, the SKU data)
        /// </summary>
        public string Data => _data;

        /// <summary>
        /// The fee to pay in order to send this transaction
        /// </summary>
        public uint Fee => _fee;

        /// <summary>
        /// The string representation of the transaction hash
        /// Does not contain dashes (-)
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
