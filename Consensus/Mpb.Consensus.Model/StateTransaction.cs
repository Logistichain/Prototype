using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class StateTransaction : AbstractTransaction
    {
        private readonly string _fromPubKey;
        private readonly string _toPubKey;
        private readonly string _skuBlockHash;
        private readonly uint _skuTxIndex;
        private readonly uint _amount;

        public string FromPubKey => _fromPubKey;
        public string ToPubKey => _toPubKey;
        /// <summary>
        /// The block where the SKU was created. If the SKU was
        /// altered, then this will be the most recent block hash to that change.
        /// </summary>
        public string SkuBlockHash => _skuBlockHash;
        /// <summary>
        /// The transaction index from the given Block (skuBlockHash) where
        /// the SKU was created or altered.
        /// If this value is 2, that means that the third transaction in SkuBlockHash
        /// refers to this SKU.
        /// </summary>
        public uint SkuTxIndex => _skuTxIndex;
        /// <summary>
        /// The amount that will be transferred in this transaction
        /// </summary>
        public uint Amount => _amount;

        public StateTransaction(string fromPubKey, string toPubKey, string skuBlockHash, uint skuTxIndex, uint amount, uint version, string action, string data, uint fee) : base(version, action, data, fee)
        {
            _fromPubKey = fromPubKey; // Can be null (coinbase)
            _toPubKey = toPubKey; // Can be null (finish supply)
            _skuBlockHash = String.IsNullOrEmpty(skuBlockHash) ? null : skuBlockHash; // Can be null (token transfer, coinbase)
            _skuTxIndex = skuTxIndex; // Is 0 in case of token transfer and coinbase
            _amount = amount;
        }
    }
}
