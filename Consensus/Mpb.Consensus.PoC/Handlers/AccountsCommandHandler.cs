using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.PoC.Handlers
{
    internal class AccountsCommandHandler
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly string _netId;

        internal AccountsCommandHandler(ITransactionRepository transactionRepository, string networkIdentifier)
        {
            _transactionRepository = transactionRepository;
            _netId = networkIdentifier;
        }

        internal void HandleCommand()
        {
            var transactions = _transactionRepository.GetAll(_netId).OfType<StateTransaction>();

            if (transactions.Count() == 0)
            {
                Console.WriteLine("No transactions found.");
            }
            else
            {
                Console.WriteLine("Balances:");
            }

            var allAdresses = transactions.Select(tx => tx.ToPubKey).Distinct();
            allAdresses = allAdresses.Concat(transactions.Select(tx => tx.FromPubKey).Distinct()).Where(address => address != null).Distinct();
            foreach (var address in allAdresses)
            {
                var transactionCount = transactions.Where(tx => tx.ToPubKey == address || tx.FromPubKey == address).Count();
                var addressBalance = _transactionRepository.GetTokenBalanceForPubKey(address, _netId);
                Console.WriteLine("------- ACCOUNT -------");
                Console.WriteLine("Public key: " + address);
                Console.WriteLine("Transactions: " + transactionCount);
                Console.WriteLine("Total balance: " + addressBalance);
                Console.WriteLine("-------=========-------");
            }
            Console.Write("> ");
        }
    }
}
