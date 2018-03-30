using System.Collections.Generic;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.TransactionLogic
{
    // Todo update documentation
    public interface ITransactionValidator
    {
        string CalculateMerkleRoot(ICollection<AbstractTransaction> orderedTransactions);
        void ValidateTransaction(AbstractTransaction tx);
        void ValidateTransaction(AbstractTransaction tx, bool checkBalance);
    }
}