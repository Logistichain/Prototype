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
        private DateTime _started;

        internal TransactionGeneratorCommandHandler(Miner miner, ITransactionCreator txCreator, ISkuRepository skuRepo, IBlockchainRepository blockchainRepo)
        {
            _miner = miner;
            _txCreator = txCreator;
            _skuRepo = skuRepo;
            _blockchainRepo = blockchainRepo;
        }

        internal void HandleStartCommand(bool startMining)
        {
            _started = DateTime.Now;
            _cts = new CancellationTokenSource();
            var keyGen = new KeyGenerator();

            Task.Run(async () =>
            {
                Random rnd = new Random();
                int skuAmount = 1000;//rnd.Next(100000, 250000);
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
                    await Task.Delay(20);
                }
            }
            );
        }


        internal void HandleStopCommand()
        {
            _cts.Cancel();
            Console.WriteLine("Transaction generator stopped.");
            Console.WriteLine("Started: " + _started.ToLongTimeString());
            Console.WriteLine("Stopped: " + DateTime.Now.ToLongTimeString());
            Console.WriteLine("Time elapsed: " + (DateTime.Now - _started).TotalMinutes + " min");
        }
    }
}
