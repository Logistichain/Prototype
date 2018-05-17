using Mpb.Consensus.Cryptography;
using Mpb.Consensus.TransactionLogic;
using Mpb.DAL;
using Mpb.Model;
using Mpb.Shared.Constants;
using Mpb.Shared.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mpb.Node.Handlers
{
    internal class TransactionGeneratorCommandHandler
    {
        private readonly Miner _miner;
        private readonly ITransactionCreator _txCreator;
        private readonly ISkuRepository _skuRepo;
        private readonly IBlockchainRepository _blockchainRepo;
        private CancellationTokenSource _cts;

        internal TransactionGeneratorCommandHandler(Miner miner, ITransactionCreator txCreator, ISkuRepository skuRepo, IBlockchainRepository blockchainRepo)
        {
            _miner = miner;
            _txCreator = txCreator;
            _skuRepo = skuRepo;
            _blockchainRepo = blockchainRepo;
        }

        internal void HandleStartCommand(bool startMining)
        {
            _cts = new CancellationTokenSource();
            var keyGen = new KeyGenerator();

            Task.Run(() =>
            {
                Random rnd = new Random();
                int skuAmount = rnd.Next(100000, 250000);
                Console.WriteLine("Preparing " + skuAmount + " SKU's..");
                List<AbstractTransaction> skuTransactions = new List<AbstractTransaction>();
                List<AbstractTransaction> transferTransactions = new List<AbstractTransaction>();
                Dictionary<string, string> keys = new Dictionary<string, string>();

                for (int i = 0; i < skuAmount; i++)
                {
                    if (_cts.IsCancellationRequested) return;

                    Random rndEan = new Random();
                    string name = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 8);
                    string eanCode = rnd.Next(100000000, 999999999).ToString(); // Should be 13 numbers long, but ok.
                    string description = " Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim.";
                    SkuData skuData = new SkuData(name, eanCode, description);
                    keyGen.GenerateKeys(out var publicKey, out var privateKey);
                    var tx = _txCreator.CreateSkuCreationTransaction(publicKey, privateKey, (uint)rndEan.Next(10, 20000), skuData);
                    try
                    {
                        keys.Add(publicKey, privateKey);
                    }
                    catch (Exception) { }
                    skuTransactions.Add(tx);
                }

                Console.WriteLine("Prepared " + skuTransactions.Count + " new SKU's. Submitting them gradually now..");

                if (startMining)
                {
                    _miner.StartMining();
                }

                foreach(var transaction in skuTransactions)
                {
                    if (_cts.IsCancellationRequested) return;
                    _miner.AddTransactionToPool(transaction, true);
                }

                while (true)
                {
                    var autoresetFlag = new AutoResetEvent(false);
                    EventPublisher.GetInstance().OnValidatedBlockCreated += (object sender, BlockCreatedEventArgs ev) =>
                    {
                        if (ev.Block.Transactions.Count() == 1)
                        {
                            autoresetFlag.Set();
                        }
                    };

                    autoresetFlag.WaitOne();

                    var destroyedCount = 0;
                    var allSkus = _skuRepo.GetAllWithHistory(_miner.NetworkIdentifier);
                    foreach (var skuWithHistory in allSkus)
                    {
                        var createSkuTransaction = (StateTransaction)skuWithHistory.First().Transaction;
                        var createSkuBlock = _blockchainRepo.GetBlockByTransactionHash(createSkuTransaction.Hash, _miner.NetworkIdentifier);
                        var createSkuTxIndex = 0;

                        for(int skuTxIndex = 0; skuTxIndex < createSkuBlock.Transactions.Count(); skuTxIndex++)
                        {
                            if (createSkuBlock.Transactions.ElementAt(skuTxIndex).Hash == createSkuTransaction.Hash)
                            {
                                createSkuTxIndex = skuTxIndex;
                                break;
                            }
                        }

                        var lastTransaction = (StateTransaction)skuWithHistory.Last().Transaction;
                        var currentSupply = _skuRepo.GetSupplyBalanceForPubKey(lastTransaction.ToPubKey, createSkuBlock.Header.Hash, createSkuTxIndex, _miner.NetworkIdentifier);
                        var isDestroyed = skuWithHistory.Where(sku => sku.Transaction.Action == TransactionAction.DestroySupply.ToString()).Any();

                        if (!isDestroyed)
                        {
                            var rand = new Random();
                            if (rand.NextDouble() >= 0.5)
                            {
                                var tx = _txCreator.CreateSupplyDestroyTransaction(
                                    lastTransaction.ToPubKey,
                                    keys.Where(k => k.Key == lastTransaction.ToPubKey).First().Value,
                                    (uint)currentSupply, // todo length check
                                    createSkuBlock.Header.Hash,
                                    createSkuTxIndex,
                                    "");
                                _miner.AddTransactionToPool(tx, true);
                            }
                            else
                            {
                                keyGen.GenerateKeys(out var publicKey, out var privateKey);
                                var tx = _txCreator.CreateSupplyTransferTransaction(
                                    lastTransaction.ToPubKey,
                                    keys.Where(k => k.Key == lastTransaction.ToPubKey).First().Value,
                                    publicKey,
                                    (uint)currentSupply,
                                    createSkuBlock.Header.Hash,
                                    createSkuTxIndex,
                                    "");
                                keys.Add(publicKey, privateKey);
                                transferTransactions.Add(tx);
                                _miner.AddTransactionToPool(tx, true);
                            }
                        }
                        else
                        {
                            destroyedCount++;
                            if (destroyedCount == allSkus.Count())
                            {
                                return;
                            }
                        }
                    }
                }
            }
            );
        }


        internal void HandleStopCommand()
        {
            _cts.Cancel();
            Console.WriteLine("Transaction generator stopped.");
        }
    }
}
