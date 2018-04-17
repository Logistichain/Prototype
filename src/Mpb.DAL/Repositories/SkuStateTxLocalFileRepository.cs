using System;
using System.Collections.Generic;
using System.Linq;
using Mpb.Model;
using Newtonsoft.Json;
using Mpb.Shared.Constants;

namespace Mpb.DAL
{
    public class SkuStateTxLocalFileRepository : ISkuRepository
    {
        private readonly IBlockchainRepository _blockchainRepo;
        private readonly ITransactionRepository _transactionRepo;

        public SkuStateTxLocalFileRepository(IBlockchainRepository blockchainRepo, ITransactionRepository transactionRepo)
        {
            _blockchainRepo = blockchainRepo;
            _transactionRepo = transactionRepo;
        }

        // todo add parameter 'includeTxPool' so StateTransactionValidator can correctly check balances
        public ulong GetSupplyBalanceForPubKey(string publicKey, string createdSkuBlockHash, int skuTxIndex, string netId)
        {
            var skuHistory = GetSkuWithHistory(createdSkuBlockHash, skuTxIndex, netId).ToList();
            return GetSupplyBalanceForPubKey(publicKey, skuHistory);
        }

        public ulong GetSupplyBalanceForPubKey(string publicKey, IEnumerable<Sku> skuHistory)
        {
            ulong totalReceived = 0;
            ulong totalSpent = 0;
            foreach (Sku skuChange in skuHistory)
            {
                var tx = (StateTransaction)skuChange.Transaction;
                if (tx.FromPubKey == publicKey && tx.Action != TransactionAction.CreateSupply.ToString() && tx.Action != TransactionAction.CreateSku.ToString())
                {
                    totalSpent += tx.Amount;
                }

                if (tx.ToPubKey == publicKey)
                {
                    totalReceived += tx.Amount;
                }
            }

            return totalReceived - totalSpent;
        }

        public IEnumerable<IEnumerable<Sku>> GetAllWithHistory(string netId)
        {
            var createTransactions = _transactionRepo
                                        .GetAll(netId)
                                        .Where(tx => tx.Action == TransactionAction.CreateSku.ToString())
                                        .OfType<StateTransaction>();
            foreach(var createTx in createTransactions)
            {
                var createTxBlock = _blockchainRepo.GetBlockByTransactionHash(createTx.Hash, netId);
                var txIndex = createTxBlock.Transactions.ToList().IndexOf(createTx);
                yield return GetSkuWithHistory(createTxBlock.Hash, txIndex, netId);
            }
        }

        public IEnumerable<Sku> GetSkuWithHistory(string createdSkuBlockHash, int skuTxIndex, string netId)
        {
            var transactions = _transactionRepo.GetAll(netId).OfType<StateTransaction>();
            var skuHistoryList = new List<Sku>();
            var skuCreationBlock = _blockchainRepo
                                        .GetBlockByHash(createdSkuBlockHash, netId);
            var skuCreationTransaction = skuCreationBlock
                                        .Transactions.OfType<StateTransaction>()
                                        .ToList()
                                        [skuTxIndex];
            var skuCreationData = JsonConvert.DeserializeObject<SkuData>(skuCreationTransaction.Data);
            var allOtherSkuTransactions = transactions.Where(tx =>
                                        tx.SkuBlockHash == createdSkuBlockHash
                                        && tx.SkuTxIndex == skuTxIndex
                                        && (tx.Action == TransactionAction.ChangeSku.ToString()
                                            || tx.Action == TransactionAction.CreateSupply.ToString()
                                            || tx.Action == TransactionAction.TransferSupply.ToString()
                                            || tx.Action == TransactionAction.DestroySupply.ToString())
                                        );

            skuHistoryList.Add(new Sku(skuCreationBlock, skuCreationTransaction, skuCreationData));
            // Fill the list with transactions about this SKU
            foreach (var skuTransaction in allOtherSkuTransactions)
            {
                // todo handle repo exceptions
                var block = _blockchainRepo.GetBlockByTransactionHash(skuTransaction.Hash, netId);
                try
                {
                    var skuChange = new Sku(block, skuTransaction, JsonConvert.DeserializeObject<SkuData>(skuTransaction.Data));
                    skuHistoryList.Add(skuChange);
                }
                catch(JsonReaderException)
                {
                    var skuChange = new Sku(block, skuTransaction, skuCreationData);
                    skuHistoryList.Add(skuChange);
                }
            }

            return skuHistoryList;
        }
    }
}
