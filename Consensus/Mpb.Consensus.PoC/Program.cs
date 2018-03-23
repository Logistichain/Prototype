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

namespace Mpb.Consensus.PoC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var persistence = new BlockchainPersistence();
            var blockHeaderHelper = new BlockHeaderHelper();
            var validator = new PowBlockValidator(blockHeaderHelper);
            var timestamper = new UnixTimestamper();
            var difficultyCalculator = new DifficultyCalculator();
            var miner = new PowBlockCreator(timestamper, validator, blockHeaderHelper);
            var logger = CreateLogger();
            var transactions = new List<Transaction>(); // Just an empty list of transactions to put in the block.
            
            BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            BigDecimal difficulty = 1;
            var secondsPerBlockGoal = 3;
            var difficultyUpdateCycle = 5;
            int createdBlocks = 0;

            // Get the local blockchain copy
            var blockchain = persistence.FindLocalBlockchain("testnet");
            logger.Information("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight);
            logger.Information("We want to achieve a total of {0} seconds for each {1} blocks to be created.", (secondsPerBlockGoal * difficultyUpdateCycle), difficultyUpdateCycle);

            logger.Information("Mining for blocks..");
            while (createdBlocks < 102)
            {
                // Every 10 blocks, recalculate the difficulty and save the blockchain.
                if (blockchain.CurrentHeight % difficultyUpdateCycle == 0 && blockchain.CurrentHeight > 0)
                {
                    difficulty = difficultyCalculator.CalculateDifficulty(blockchain, blockchain.CurrentHeight, 1, secondsPerBlockGoal, difficultyUpdateCycle);
                    persistence.SaveBlockchain(blockchain);
                    logger.Information("Blockchain persisted.");
                    var difficultyInfo = difficultyCalculator.GetPreviousDifficultyUpdateInformation(blockchain, difficultyUpdateCycle);
                    logger.Information("Total time to create blocks {0}-{1}: {2} sec", difficultyInfo.BeginHeight, difficultyInfo.EndHeight - 1, difficultyInfo.TotalSecondsForBlocks);
                    logger.Debug("Difficulty for next block {0}", difficulty);
                    logger.Debug("Target for next block {0}", difficulty);
                }

                logger.Debug("Current height: {0}", blockchain.CurrentHeight);
                var newBlock = miner.CreateValidBlock(transactions, difficulty);
                blockchain.Blocks.Add(newBlock);
                logger.Information("Found a new block!");
                createdBlocks++;
            }
            logger.Information("Mined 80 blocks. Mining stopped.");

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
