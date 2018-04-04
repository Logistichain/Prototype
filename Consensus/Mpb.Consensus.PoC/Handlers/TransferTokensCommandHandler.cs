using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Logic.TransactionLogic;
using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.PoC.Handlers
{
    internal class TransferTokensCommandHandler
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly ITransactionCreator _transactionCreator;

        internal TransferTokensCommandHandler(ITransactionRepository transactionRepo, ITransactionCreator transactionCreator)
        {
            _transactionRepo = transactionRepo;
            _transactionCreator = transactionCreator;
        }

        internal void HandleCommand(Miner miner)
        {
            uint tokenFee = 10; // From BlockchainConstants.cs
            Console.WriteLine("Current transfer token fee is " + tokenFee + " TK.");
            WriteLineWithInputCursor("Enter the sender's public key:");
            var fromPub = Console.ReadLine().ToLower();
            var balance = _transactionRepo.GetTokenBalanceForPubKey(fromPub, miner.NetworkIdentifier);

            Console.WriteLine("The sender's balance: " + balance);
            WriteLineWithInputCursor("Enter the sender's private key (can be anything for now):");
            var fromPriv = Console.ReadLine().ToLower();

            WriteLineWithInputCursor("Enter the receiver's public key:");
            var toPub = Console.ReadLine().ToLower();

            // Todo support custom fees in transactionCreator
            /*
            var askFeeFirstTime = true;
            var forceLowerFee = false;
            while (tokenFee < 10 && !forceLowerFee || askFeeFirstTime)
            {
                askFeeFirstTime = false;
                WriteLineWithInputCursor("Use a different fee [10]:");
                var feeInput = Console.ReadLine().ToLower();
                while (!UInt32.TryParse(feeInput, out tokenFee))
                {
                    tokenFee = 10;
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

                if (tokenFee < 10 && !forceLowerFee)
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

            uint amount = 0;
            bool forceAmount = false;
            while (amount < 1 || amount > balance && !forceAmount)
            {
                Console.WriteLine("Enter the amount to send:");

                var amountInput = Console.ReadLine().ToLower();
                while (!UInt32.TryParse(amountInput, out amount))
                {
                    WriteLineWithInputCursor("Invalid value. Use a positive numeric value without decimals.");
                    amountInput = Console.ReadLine().ToLower();
                }

                if (amount + tokenFee > balance && !forceAmount)
                {
                    Console.WriteLine("The given amount + fee is higher than the sender's balance and can cause a rejection.");
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
            
            AbstractTransaction transactionToSend = _transactionCreator.CreateTokenTransferTransaction(fromPub, fromPriv, toPub, amount, optionalData);
            miner.AddTransactionToPool(transactionToSend);
            Console.Write("> ");
        }
        
        private void WriteLineWithInputCursor(string writeLine)
        {
            Console.WriteLine(writeLine);
            Console.Write("> ");
        }
    }
}
