using System;
using System.Collections.Generic;
using System.Linq;
using Mpb.Consensus.Logic.Constants;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.DAL
{
    public class StateTransactionLocalFileRepository : ITransactionRepository
    {
        private Blockchain _trackingBlockchain;

        public StateTransactionLocalFileRepository (Blockchain blockchainSource)
        {
            _trackingBlockchain = blockchainSource;
        }

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
            return GetAllByPredicate(tx => true);
        }

        public IEnumerable<AbstractTransaction> GetAllByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.FromPubKey == pubKey || tx.ToPubKey == pubKey);
        }

        public IEnumerable<AbstractTransaction> GetAllSentByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.FromPubKey == pubKey);
        }

        public IEnumerable<AbstractTransaction> GetAllReceivedByPublicKey(string pubKey, string netId)
        {
            return GetAllByPredicate(tx => tx.ToPubKey == pubKey);
        }

        private IEnumerable<StateTransaction> GetAllByPredicate(Func<StateTransaction, bool> predicate)
        {
            foreach (Block b in _trackingBlockchain.Blocks)
            {
                if (b.Version > BlockchainConstants.ProtocolVersion)
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
