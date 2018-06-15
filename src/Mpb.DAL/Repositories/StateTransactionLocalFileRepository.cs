using System;
using System.Collections.Generic;
using System.Linq;
using Mpb.Model;
using Mpb.Shared.Constants;

namespace Mpb.DAL
{
    public class StateTransactionLocalFileRepository : ITransactionRepository
    {
        private readonly IBlockchainRepository _blockchainRepo;

        public StateTransactionLocalFileRepository(IBlockchainRepository blockchainRepository)
        {
            _blockchainRepo = blockchainRepository;
        }

        // todo add parameter 'includeTxPool' so StateTransactionValidator can correctly check balances
        public ulong GetTokenBalanceForPubKey(string publicKey, string netId)
        {
            ulong totalReceived = 0;
            ulong totalSpent = 0;
            var tokenTransactions = GetAllByPublicKey(publicKey, netId)
                                    .Where(tx =>
                                        tx.Action == TransactionAction.ClaimCoinbase.ToString()
                                        || tx.Action == TransactionAction.TransferToken.ToString())
                                    .OfType<StateTransaction>()
                                    .ToList();
            var otherTransactions = GetAllByPublicKey(publicKey, netId)
                                    .Where(tx =>
                                        tx.Action != TransactionAction.ClaimCoinbase.ToString()
                                        && tx.Action != TransactionAction.TransferToken.ToString())
                                    .OfType<StateTransaction>()
                                    .ToList();
            foreach (StateTransaction tx in tokenTransactions)
            {
                if (tx.FromPubKey == publicKey)
                {
                    totalSpent += tx.Amount + tx.Fee;
                }

                if (tx.ToPubKey == publicKey)
                {
                    totalReceived += tx.Amount;
                }
            }

            foreach (StateTransaction tx in otherTransactions)
            {
                if (tx.FromPubKey == publicKey)
                {
                    totalSpent += tx.Fee;
                }
            }

            return totalReceived - totalSpent;
        }

        public IEnumerable<AbstractTransaction> GetAll(string netId)
        {
            return GetAllByPredicate(tx => true, netId);
        }

        public IEnumerable<AbstractTransaction> GetAllByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.FromPubKey == pubKey || tx.ToPubKey == pubKey, netId);
        }

        public IEnumerable<AbstractTransaction> GetAllSentByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.FromPubKey == pubKey, netId);
        }

        public IEnumerable<AbstractTransaction> GetAllReceivedByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.ToPubKey == pubKey, netId);
        }

        private IEnumerable<StateTransaction> GetAllByPredicate(Func<StateTransaction, bool> predicate, string netId)
        {
            var blockchain = _blockchainRepo.GetChainByNetId(netId);
            lock (blockchain)
            {
                foreach (Block b in blockchain.Blocks)
                {
                    if (b.Header.Version > BlockchainConstants.ProtocolVersion)
                    {
                        // todo log unsupporting block version found, but continue
                    }

                    var stateTransactions = b.Transactions.OfType<StateTransaction>();
                    var transactionsByPubKey = stateTransactions.Where(predicate);
                    foreach (StateTransaction tx in transactionsByPubKey)
                    {
                        yield return tx;
                    }
                }
            }
        }
    }
}
