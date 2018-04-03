using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.Model
{
    /// <summary>
    /// This model is used to corretly deserialize JSON blocks.
    /// Without this model, the JSON serializer tries to instantiate a new AbstractTransaction.
    /// </summary>
    public class BlockWithStateTransactions : Block
    {
        public new IEnumerable<StateTransaction> Transactions => _transactions.OfType<StateTransaction>();

        public BlockWithStateTransactions(string hash, ulong nonce, string magicNumber, uint version, string merkleRoot, long timestamp, IEnumerable<StateTransaction> transactions) : base(magicNumber, version, merkleRoot, timestamp, transactions)
        {
            this.IncrementNonce(nonce);
            this.SetHash(hash);
        }
    }
}
