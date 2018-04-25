using System.Collections.Generic;
using Mpb.Model;

namespace Mpb.Consensus.TransactionLogic
{
    /// <summary>
    /// Validate transactions before accepting them.
    /// </summary>
    public interface ITransactionValidator
    {
        /// <summary>
        /// Calculate the merkleroot for a set of transactions
        /// <seealso cref="https://bitcoin.org/en/glossary/merkle-root"/>
        /// </summary>
        /// <param name="orderedTransactions">The set of transactions to calculate the merkleroot of</param>
        /// <returns>The merkleroot hash, uppercase, without dashes</returns>
        string CalculateMerkleRoot(ICollection<AbstractTransaction> orderedTransactions);

        /// <summary>
        /// Validates a transaction, including balance checks, on the current network.
        /// Throws TransactionRejectedException if the validation fails.
        /// </summary>
        /// <param name="tx">The transaction to validate</param>
        void ValidateTransaction(AbstractTransaction tx);

        /// <summary>
        /// Validates a transaction, including balance checks, on a specfied network.
        /// Throws TransactionRejectedException if the validation fails.
        /// </summary>
        /// <param name="tx">The transaction to validate</param>
        /// <param name="netIdentifier">The network where the transaction will be placed in</param>
        void ValidateTransaction(AbstractTransaction tx, string netIdentifier);
    }
}