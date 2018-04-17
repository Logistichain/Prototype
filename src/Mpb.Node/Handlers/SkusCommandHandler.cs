using Mpb.DAL;
using Mpb.Consensus.MiscLogic;
using Mpb.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpb.Shared.Constants;

namespace Mpb.Node.Handlers
{
    internal class SkusCommandHandler
    {
        private readonly IBlockchainRepository _blockchainRepo;
        private readonly ITimestamper _timestamper;
        private readonly ISkuRepository _skuRepository;
        private readonly string _netId;

        internal SkusCommandHandler(IBlockchainRepository blockchainRepo, ITimestamper timestamper, ISkuRepository skuRepository, string networkIdentifier)
        {
            _blockchainRepo = blockchainRepo;
            _timestamper = timestamper;
            _skuRepository = skuRepository;
            _netId = networkIdentifier;
        }

        internal void HandleCommand()
        {
            var currentBlockchain = _blockchainRepo.GetChainByNetId(_netId);
            var historyLists = _skuRepository.GetAllWithHistory(_netId);

            if (historyLists.Count() == 0)
            {
                Console.WriteLine("No transactions found.");
            }
            else
            {
                Console.WriteLine("SKU's:");
            }

            // Now loop though all the SKU's historylists
            foreach (List<Sku> historyList in historyLists)
            {
                Console.WriteLine("------- SKU -------");
                var createSkuTransaction = (StateTransaction)historyList.First().Transaction;
                var currentSupply = createSkuTransaction.Amount;
                var owner = createSkuTransaction.FromPubKey;
                var versionNumber = 0;
                foreach (var skuversion in historyList)
                {
                    var blockHeight = _blockchainRepo.GetHeightForBlock(skuversion.Block.Hash, _netId);
                    var confirmations = (currentBlockchain.CurrentHeight - blockHeight) + 1; // The block itself is a confirmation aswell.
                    var blockTime = _timestamper.GetUtcDateTimeFromTimestamp(skuversion.Block.Timestamp);
                    var transaction = (StateTransaction)skuversion.Transaction;
                    var isCreateOrChange = transaction.Action == TransactionAction.CreateSku.ToString() || transaction.Action == TransactionAction.ChangeSku.ToString();

                    if (isCreateOrChange)
                    {
                        versionNumber++;
                    }

                    Console.WriteLine("Action: " + transaction.Action);
                    Console.WriteLine("SKU Version: " + versionNumber);
                    Console.WriteLine("Time: " + blockTime.ToString("dd-MM-yyy HH:mm:ss") + " UTC time");
                    Console.WriteLine("Block hash: " + skuversion.Block.Hash);
                    Console.WriteLine("Block height: " + blockHeight);
                    Console.WriteLine("Confirmations: " + confirmations.ToString());
                    Console.WriteLine("Transaction hash: " + transaction.Hash);
                    if (isCreateOrChange)
                    {
                        Console.WriteLine("  Owner pubkey: " + owner);
                        Console.WriteLine("  SKU ID: " + skuversion.Data.SkuId);
                        Console.WriteLine("  EAN code: " + skuversion.Data.EanCode);
                        Console.WriteLine("  Description: " + skuversion.Data.Description);
                    }
                    else if (transaction.Action == TransactionAction.DestroySupply.ToString())
                    {
                        currentSupply -= transaction.Amount;
                    }
                    else if (transaction.Action == TransactionAction.CreateSupply.ToString())
                    {
                        currentSupply -= transaction.Amount;
                    }
                    else if (transaction.Action == TransactionAction.TransferSupply.ToString())
                    {
                        Console.WriteLine("  From: " + transaction.FromPubKey);
                        Console.WriteLine("  To: " + transaction.ToPubKey);
                        Console.WriteLine("  Amount: " + transaction.Amount);
                    }
                    Console.WriteLine("Total supply after transaction: " + currentSupply);
                    if (versionNumber != historyList.Count)
                    {
                        Console.WriteLine("-------");
                    }
                }
                Console.WriteLine("-------=====-------");
            }
            Console.Write("> ");
        }
    }
}
