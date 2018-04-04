using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.PoC.Handlers
{
    internal class TransactionPoolCommandHandler
    {
        internal void HandleCommand(IEnumerable<AbstractTransaction> transactionPool)
        {
            var allPendingTransactions = transactionPool.OfType<StateTransaction>();

            if (allPendingTransactions.Count() == 0)
            {
                Console.WriteLine("No pending transactions.");
            }
            else
            {
                Console.WriteLine("Pending transactions:");
            }

            foreach (var transaction in allPendingTransactions)
            {
                Console.WriteLine("----- PENDING TRANSACTION -----");
                Console.WriteLine("Hash: " + transaction.Hash);
                Console.WriteLine("TxVersion: " + transaction.Version);
                Console.WriteLine("From: " + transaction.FromPubKey ?? "<unknown>");
                Console.WriteLine("To: " + transaction.ToPubKey ?? "<unknown>");
                Console.WriteLine("Action: " + transaction.Action);
                Console.WriteLine("Fee: " + transaction.Fee + " TK");
                Console.WriteLine("Amount: " + transaction.Amount);
                Console.WriteLine("SkuBlockHash: " + transaction.SkuBlockHash ?? "Not applicable");
                Console.WriteLine("SkuTxIndex: " + transaction.SkuTxIndex ?? "Not applicable");
                Console.WriteLine("Data: " + transaction.Data);
                Console.WriteLine("-----=====================-----");
            }
            Console.Write("> ");
        }
    }
}
