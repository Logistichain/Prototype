using System.Collections.Generic;
using Mpb.Model;

namespace Mpb.Consensus.TransactionLogic
{
    // Todo update documentation
    public interface ITransactionValidator
    {
        string CalculateMerkleRoot(ICollection<AbstractTransaction> orderedTransactions);
        void ValidateTransaction(AbstractTransaction tx);
        void ValidateTransaction(AbstractTransaction tx, string netIdentifier, bool checkBalance);
    }
}