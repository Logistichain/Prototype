﻿using Logistichain.DAL;
using Logistichain.Consensus.TransactionLogic;
using Logistichain.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Logistichain.Shared.Events;
using System.Linq;

namespace Logistichain.Node.Handlers
{
    internal class TransferSupplyCommandHandler
    {
        private readonly ISkuRepository _skuRepository;
        private readonly ITransactionRepository _transactionRepo;
        private readonly ITransactionCreator _transactionCreator;
        private readonly string _netId;
        private const ulong TransferSupplyFee = 1;

        internal TransferSupplyCommandHandler(ISkuRepository skuRepository, ITransactionRepository transactionRepo, ITransactionCreator transactionCreator, string netId)
        {
            _skuRepository = skuRepository;
            _transactionRepo = transactionRepo;
            _transactionCreator = transactionCreator;
            _netId = netId;
        }

        internal void HandleCommand()
        {
            ulong transferFee = TransferSupplyFee; // From BlockchainConstants.cs
            IEnumerable<Sku> skuWithHistory = null;
            var blockHash = "";
            var txIndex = 0;

            Console.WriteLine("Current transfer supply fee is " + TransferSupplyFee + " TK.");
            while (skuWithHistory == null)
            {
                WriteLineWithInputCursor("Enter the block hash where the SKU was created:");
                blockHash = Console.ReadLine().ToUpper();

                WriteLineWithInputCursor("Enter index of the transaction where the SKU resides:");
                var txIndexInput = Console.ReadLine().ToLower();
                Int32.TryParse(txIndexInput, out txIndex);

                try
                {
                    if (blockHash == "LAST")
                    {
                        skuWithHistory = _skuRepository.GetAllWithHistory(_netId).Last();
                        blockHash = skuWithHistory.First().Block.Header.Hash;
                    }
                    else
                    {
                        skuWithHistory = _skuRepository.GetSkuWithHistory(blockHash, txIndex, _netId);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("The block or transaction could not be found. Try again.");
                }
            }

            Console.WriteLine("Selected " + skuWithHistory.First().Data.SkuId);
            WriteLineWithInputCursor("Enter the sender's public key:");
            var fromPub = Console.ReadLine();

            var fromPriv = Program.GetPrivKey(fromPub);
            while (String.IsNullOrWhiteSpace(fromPriv))
            {
                Console.WriteLine("Private key not found.");
                WriteLineWithInputCursor("Enter the sender's public key:");
                fromPub = Console.ReadLine();
            }

            var senderBalance = _transactionRepo.GetTokenBalanceForPubKey(fromPub, _netId);
            var skuSupply = _skuRepository.GetSupplyBalanceForPubKey(fromPub, skuWithHistory);

            Console.WriteLine("The sender's token balance: " + senderBalance);
            Console.WriteLine("The sender's supply: " + skuSupply);


            WriteLineWithInputCursor("Enter the receiver's public key:");
            var toPub = Console.ReadLine();


            // Todo support custom fees in transactionCreator
            /*
            var askFeeFirstTime = true;
            var forceHigherFee = false;
            while (transferFee < TransferSupplyFee && !forceHigherFee || askFeeFirstTime)
            {
                askFeeFirstTime = false;
                WriteLineWithInputCursor("Use a different fee ["+ TransferSupplyFee + "]:");
                var feeInput = Console.ReadLine().ToLower();
                while (!ulong.TryParse(feeInput, out transferFee))
                {
                    transferFee = TransferSupplyFee;
                    if (feeInput != "")
                    {
                        WriteLineWithInputCursor("Invalid value. Use a positive numeric value without decimals.");
                        feeInput = Console.ReadLine().ToLower();
                    }
                    else
                    {
                        break;
                    }
                }

                if (transferFee > senderBalance && !forceHigherFee)
                {
                    Console.WriteLine("The given fee is higher than the sender's token balance and can cause a rejection.");
                    WriteLineWithInputCursor("Type 'force' to use the given amount. Press ENTER to specify another amount.");
                    feeInput = Console.ReadLine().ToLower();
                    if (feeInput == "force")
                    {
                        forceHigherFee = true;
                    }
                }
            }
            */

            uint amount = 0;
            bool forceAmount = false;
            while (amount < 1 || amount > skuSupply && !forceAmount)
            {
                WriteLineWithInputCursor("Specify the amount to transfer:");

                var amountInput = Console.ReadLine().ToLower();
                while (!UInt32.TryParse(amountInput, out amount))
                {
                    WriteLineWithInputCursor("Invalid value. Use a positive numeric value without decimals.");
                    amountInput = Console.ReadLine().ToLower();
                }

                if (amount > skuSupply && !forceAmount)
                {
                    Console.WriteLine("The given amount is higher than the sender's current supply balance and can cause a rejection.");
                    WriteLineWithInputCursor("Type 'force' to use the given amount. Press ENTER to specify another amount.");
                    amountInput = Console.ReadLine().ToLower();
                    if (amountInput == "force")
                    {
                        forceAmount = true;
                    }
                }
            }

            WriteLineWithInputCursor("Enter optional data []:");
            var optionalData = Console.ReadLine();
            
            AbstractTransaction transactionToSend = _transactionCreator.CreateSupplyTransferTransaction(fromPub, fromPriv, toPub, amount, blockHash, txIndex, optionalData);
            EventPublisher.GetInstance().PublishUnvalidatedTransactionReceived(this, new TransactionReceivedEventArgs(transactionToSend));
            Console.Write("> ");
        }

        private void WriteLineWithInputCursor(string writeLine)
        {
            Console.WriteLine(writeLine);
            Console.Write("> ");
        }
    }
}
