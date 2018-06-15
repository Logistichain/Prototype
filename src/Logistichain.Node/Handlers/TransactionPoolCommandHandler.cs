using Logistichain.Consensus.TransactionLogic;
using Logistichain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logistichain.Node.Handlers
{
    internal class TransactionPoolCommandHandler
    {
        internal void HandleCommand(ConcurrentTransactionPool transactionPool)
        {
            var allPendingStateTransactions = transactionPool.GetAllTransactions().OfType<StateTransaction>();

            if (allPendingStateTransactions.Count() == 0)
            {
                Console.WriteLine("No pending transactions.");
            }
            else
            {
                Console.WriteLine("Pending transactions:");
            }

            foreach (var transaction in allPendingStateTransactions)
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
