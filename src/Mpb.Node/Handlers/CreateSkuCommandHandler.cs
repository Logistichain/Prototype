using Mpb.DAL;
using Mpb.Consensus.TransactionLogic;
using Mpb.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Node.Handlers
{
    internal class CreateSkuCommandHandler
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly ITransactionCreator _transactionCreator;
        private const ulong CreateSkuFee = 100;

        internal CreateSkuCommandHandler(ITransactionRepository transactionRepo, ITransactionCreator transactionCreator)
        {
            _transactionRepo = transactionRepo;
            _transactionCreator = transactionCreator;
        }

        internal void HandleCommand(Miner miner)
        {
            ulong creationFee = CreateSkuFee; // From BlockchainConstants.cs
            Console.WriteLine("Current SKU creation fee is " + creationFee + " TK.");
            WriteLineWithInputCursor("Enter the sender's public key:");
            var fromPub = Console.ReadLine().ToLower();
            var balance = _transactionRepo.GetTokenBalanceForPubKey(fromPub, miner.NetworkIdentifier);

            Console.WriteLine("The sender's balance: " + balance);
            WriteLineWithInputCursor("Enter the sender's private key (can be anything for now):");
            var fromPriv = Console.ReadLine().ToLower();

            // Todo support custom fees in transactionCreator
            /*
            var askFeeFirstTime = true;
            var forceLowerFee = false;
            while (creationFee < CreateSkuFee && !forceLowerFee || askFeeFirstTime)
            {
                askFeeFirstTime = false;
                WriteLineWithInputCursor("Use a different fee ["+ CreateSkuFee + "]:");
                var feeInput = Console.ReadLine().ToLower();
                while (!ulong.TryParse(feeInput, out creationFee))
                {
                    creationFee = CreateSkuFee;
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

                if (creationFee < CreateSkuFee && !forceLowerFee)
                {
                    Console.WriteLine("This low fee might result into a rejection. ");
                    WriteLineWithInputCursor("Type 'force' to use the given fee. Press ENTER to specify another amount.");
                    feeInput = Console.ReadLine().ToLower();
                    if (feeInput == "force")
                    {
                        forceLowerFee = true;
                    }
                }
            }
            */

            var sku = HandleCreateSkuCommand();

            bool firstTimeSupplyAmountInput = true;
            uint supplyAmount = 0;
            while (supplyAmount < 0 || firstTimeSupplyAmountInput)
            {
                firstTimeSupplyAmountInput = false;
                WriteLineWithInputCursor("Specify the initial supply:");

                var amountInput = Console.ReadLine().ToLower();
                while (!UInt32.TryParse(amountInput, out supplyAmount))
                {
                    WriteLineWithInputCursor("Invalid value. Use a positive numeric value without decimals.");
                    amountInput = Console.ReadLine().ToLower();
                }
            }
            
            AbstractTransaction transactionToSend = _transactionCreator.CreateSkuCreationTransaction(fromPub, fromPriv, supplyAmount, sku);
            miner.AddTransactionToPool(transactionToSend);
            Console.Write("> ");
        }

        private SkuData HandleCreateSkuCommand()
        {
            Console.WriteLine("-- SKU information --");
            Console.WriteLine("The following fields are required to create an SKU: SKU name, Ean code, Description.");
            string skuId = "";
            while (String.IsNullOrWhiteSpace(skuId))
            {
                WriteLineWithInputCursor("Enter the SKU name:");
                skuId = Console.ReadLine();
            }

            string eanCode = "";
            while (String.IsNullOrWhiteSpace(eanCode))
            {
                WriteLineWithInputCursor("Enter the EAN Code:");
                eanCode = Console.ReadLine();
            }

            string description = "";
            while (String.IsNullOrWhiteSpace(description))
            {
                WriteLineWithInputCursor("Provide a description:");
                description = Console.ReadLine();
            }

            return new SkuData(skuId, eanCode, description);
        }
        
        private void WriteLineWithInputCursor(string writeLine)
        {
            Console.WriteLine(writeLine);
            Console.Write("> ");
        }
    }
}
