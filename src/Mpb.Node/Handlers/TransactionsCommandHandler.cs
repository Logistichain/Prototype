using Mpb.DAL;
using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Node.Handlers
{
    internal class TransactionsCommandHandler
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly string _netId;

        internal TransactionsCommandHandler(ITransactionRepository transactionRepository, string networkIdentifier)
        {
            _transactionRepository = transactionRepository;
            _netId = networkIdentifier;
        }

        internal void HandleCommand()
        {
            var allTransactions = _transactionRepository.GetAll(_netId).OfType<StateTransaction>();
            Console.WriteLine("Transactions (" + allTransactions.Count() + "):");
            foreach (var transaction in allTransactions)
            {
                Console.WriteLine("----- TRANSACTION -----");
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
                Console.WriteLine("-----=============-----");
            }
            Console.Write("> ");
        }
    }
}
