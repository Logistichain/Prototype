﻿using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.PoC.Handlers
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
            Console.WriteLine("Transactions:");
            var allTransactions = _transactionRepository.GetAll(_netId).OfType<StateTransaction>();
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
