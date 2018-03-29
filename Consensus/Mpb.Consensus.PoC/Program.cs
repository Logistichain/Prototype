using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Logic.MiscLogic;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Mpb.Consensus.Model;
using System.Reflection;
using System.IO;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using Mpb.Consensus.Logic.TransactionLogic;
using System.Linq;

namespace Mpb.Consensus.PoC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IBlockchainRepository blockchainRepo = new BlockchainLocalFileRepository();
            var networkIdentifier = "testnet";
            var blockchain = blockchainRepo.GetByNetId(networkIdentifier);
            var transactionRepo = new StateTransactionLocalFileRepository(blockchain);
            IBlockHeaderHelper blockHeaderHelper = new BlockHeaderHelper();
            ITimestamper timestamper = new UnixTimestamper();
            var transactionByteConverter = new TransactionByteConverter();
            var transactionCreator = new StateTransactionCreator(transactionByteConverter);
            var transactionValidator = new StateTransactionValidator(transactionByteConverter);
            var validator = new PowBlockValidator(blockHeaderHelper, transactionValidator, timestamper);
            var difficultyCalculator = new DifficultyCalculator();
            var miner = new PowBlockCreator(timestamper, validator, blockHeaderHelper);
            var logger = CreateLogger();
            var walletPubKey = "pubkey";
            var walletPrivKey = "privkey";

            BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            BigDecimal difficulty = 1;
            uint secondsPerBlockGoal = 3;
            var difficultyUpdateCycle = 5;
            int createdBlocks = 0;

            // Get the local blockchain copy
            logger.Information("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight);
            logger.Information("We want to achieve a total of {0} seconds for each {1} blocks to be created.", (secondsPerBlockGoal * difficultyUpdateCycle), difficultyUpdateCycle);

            logger.Information("Mining for blocks..");
            while (createdBlocks < 102)
            {
                // Every 10 blocks, recalculate the difficulty and save the blockchain.
                if (blockchain.CurrentHeight % difficultyUpdateCycle == 0 && blockchain.CurrentHeight > 0)
                {
                    difficulty = difficultyCalculator.CalculateDifficulty(blockchain, blockchain.CurrentHeight, 1, secondsPerBlockGoal, difficultyUpdateCycle);
                    blockchainRepo.Update(blockchain);
                    logger.Information("Blockchain persisted.");
                    var difficultyInfo = difficultyCalculator.GetPreviousDifficultyUpdateInformation(blockchain, difficultyUpdateCycle);
                    logger.Information("Total time to create blocks {0}-{1}: {2} sec", difficultyInfo.BeginHeight, difficultyInfo.EndHeight - 1, difficultyInfo.TotalSecondsForBlocks);
                    logger.Debug("Difficulty for next block {0}", difficulty);
                    logger.Debug("Target for next block {0}", difficulty);
                }
                logger.Debug("Current height: {0}", blockchain.CurrentHeight);

                // Calculate our current balance
                var allReceivedTransactions = transactionRepo.GetAllReceivedByPublicKey(walletPubKey, networkIdentifier);
                uint balance = 0;
                foreach(StateTransaction tx in allReceivedTransactions.OfType<StateTransaction>())
                {
                    balance += tx.Amount;
                }
                logger.Debug("Our balance: {0}", balance);

                // Create & add the coinbase transaction and then mine the block
                var coinbaseTx = transactionCreator.CreateCoinBaseTransaction(walletPubKey, walletPrivKey, "Mined by Montapacking!");
                var transactions = new List<AbstractTransaction>() { coinbaseTx };
                var newBlock = miner.CreateValidBlock(transactions, difficulty);

                blockchain.Blocks.Add(newBlock);
                logger.Information("Found a new block!");
                createdBlocks++;
            }
            logger.Information("Mined 100 blocks. Mining stopped.");

            logger.Information("Program finished. Press ENTER to close.");
            Console.ReadLine();
        }

        private static ILogger CreateLogger()
        {
            var time = DateTime.Now.Hour + "" + DateTime.Now.Minute;
            var fileLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log-" + time + ".txt");
            return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.ColoredConsole()
                    .WriteTo.File(fileLocation)
                    .CreateLogger();
        }
    }
}
