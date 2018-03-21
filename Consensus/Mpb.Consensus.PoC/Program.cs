using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Logic.MiscLogic;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.PoC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var persistence = new BlockchainPersistence();
            var timestamper = new UnixTimestamper();
            var difficultyCalculator = new DifficultyCalculator();
            var miner = new PowBlockCreator(timestamper);
            var logger = CreateLogger();

            // Get the local blockchain copy
            var blockchain = persistence.FindLocalBlockchain("testnet");
            logger.Information("Loaded blockchain. Current height: {Height}", blockchain.Blocks.Count);

            logger.Information("Mining for blocks..");
            BigDecimal difficulty = 1;
            var difficultyUpdatecycle = 10;
            int createdBlocks = 0;
            while (createdBlocks < 81)
            {
                // Every 10 blocks, recalculate the difficulty and save the blockchain.
                if (createdBlocks % difficultyUpdatecycle == 0)
                {
                    difficulty = difficultyCalculator.CalculateCurrentDifficulty(blockchain);
                    persistence.SaveBlockchain(blockchain);
                    logger.Information("Blockchain persisted.");
                }

                Thread.Sleep(1000);
                logger.Debug("Difficulty for next block {Difficulty}", difficulty);
                logger.Debug("Current height: {Height}", blockchain.Blocks.Count);
                var newBlock = miner.CreateValidBlock(difficulty);
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
            return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.ColoredConsole()
                    .CreateLogger();
        }
    }
}
