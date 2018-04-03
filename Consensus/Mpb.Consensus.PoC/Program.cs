using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.DAL;
using Serilog;
using System;
using Mpb.Consensus.Model;
using System.Reflection;
using System.IO;
using Mpb.Consensus.Logic.TransactionLogic;
using Mpb.Consensus.PoC.Handlers;
using Mpb.Consensus.Logic.MiscLogic;

namespace Mpb.Consensus.PoC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ILogger logger = CreateLogger();
            var networkIdentifier = "testnet";
            IBlockchainRepository blockchainRepo = new BlockchainLocalFileRepository();
            Blockchain blockchain = blockchainRepo.GetChainByNetId(networkIdentifier);
            ITransactionRepository transactionRepo = new StateTransactionLocalFileRepository(blockchain);
            ITransactionCreator transactionCreator = new StateTransactionCreator(new TransactionByteConverter());
            ITimestamper timestamper = new UnixTimestamper();
            ISkuRepository skuRepository = new SkuStateTxLocalFileRepository(blockchainRepo, transactionRepo);
            var walletPubKey = "montaminer";
            var walletPrivKey = "montaprivatekey";
            Miner miner = new Miner(blockchain, walletPubKey, walletPrivKey, logger);
            // Command handlers, only large commands are handles by these separate handlers.
            AccountsCommandHandler accountsCmdHandler = new AccountsCommandHandler(transactionRepo, networkIdentifier);
            SkusCommandHandler skusCmdHandler = new SkusCommandHandler(blockchainRepo, timestamper, skuRepository, networkIdentifier);
            TransactionsCommandHandler transactionsCmdHandler = new TransactionsCommandHandler(transactionRepo, networkIdentifier);
            TransactionPoolCommandHandler txpoolCmdHandler = new TransactionPoolCommandHandler();
            TransferTokensCommandHandler transferTokensCmdHandler = new TransferTokensCommandHandler(transactionRepo, transactionCreator);
            CreateSkuCommandHandler createSkuCmdHandler = new CreateSkuCommandHandler(transactionRepo, transactionCreator);
            TransferSupplyCommandHandler transferSupplyCmdHandler = new TransferSupplyCommandHandler(skuRepository, transactionRepo, transactionCreator, networkIdentifier);

            logger.Information("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
            PrintConsoleCommands();

            var input = "";
            while (input != "exit")
            {
                input = Console.ReadLine().ToLower();
                switch (input)
                {
                    case "help":
                        PrintConsoleCommands();
                        break;
                    case "accounts":
                    case "users":
                    case "balances":
                        accountsCmdHandler.HandleCommand();
                        break;
                    case "skus":
                        skusCmdHandler.HandleCommand();
                        break;
                    case "txpool":
                    case "transactionpool":
                    case "pendingtransactions":
                        txpoolCmdHandler.HandleCommand(miner.TransactionPool);
                        break;
                    case "transactions":
                        transactionsCmdHandler.HandleCommand();
                        break;
                    case "startmining":
                        miner.StartMining();
                        Console.Write("> ");
                        break;
                    case "stopmining":
                        miner.StopMining(true);
                        blockchainRepo.Update(blockchain);
                        PrintConsoleCommands();
                        break;
                    case "resetblockchain":
                        miner.StopMining(false);
                        blockchainRepo.Delete(networkIdentifier);
                        Console.WriteLine("Blockchain deleted.");
                        // Initialize all variables again because the heap references changed.
                        blockchain = blockchainRepo.GetChainByNetId(networkIdentifier);
                        transactionRepo = new StateTransactionLocalFileRepository(blockchain);
                        miner = new Miner(blockchain, walletPubKey, walletPrivKey, logger);
                        accountsCmdHandler = new AccountsCommandHandler(transactionRepo, networkIdentifier);
                        skuRepository = new SkuStateTxLocalFileRepository(blockchainRepo, transactionRepo);
                        skusCmdHandler = new SkusCommandHandler(blockchainRepo, timestamper, skuRepository, networkIdentifier);
                        transactionsCmdHandler = new TransactionsCommandHandler(transactionRepo, networkIdentifier);
                        txpoolCmdHandler = new TransactionPoolCommandHandler();
                        transferTokensCmdHandler = new TransferTokensCommandHandler(transactionRepo, transactionCreator);
                        createSkuCmdHandler = new CreateSkuCommandHandler(transactionRepo, transactionCreator);
                        logger.Information("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
                        Console.Write("> ");
                        break;
                    case "transfertokens":
                        transferTokensCmdHandler.HandleCommand(miner);
                        break;
                    case "createsku":
                        createSkuCmdHandler.HandleCommand(miner);
                        break;
                    case "transfersupply":
                        transferSupplyCmdHandler.HandleCommand(miner);
                        break;
                    default:
                        Console.WriteLine("I don't recognize that command.");
                        Console.Write("> ");
                        break;
                }
            }
        }

        private static void PrintConsoleCommands()
        {
            Console.WriteLine("----- [MontaBlockchain] -----");
            Console.WriteLine("Available commands:");
            Console.WriteLine("- help");
            Console.WriteLine("- transactions");
            Console.WriteLine("- txpool / transactionpool / pendingtransactions");
            Console.WriteLine("- accounts / users / balances");
            Console.WriteLine("- skus");
            Console.WriteLine("- createsku");
            Console.WriteLine("- transfersupply");
            Console.WriteLine("- startmining");
            Console.WriteLine("- stopmining");
            Console.WriteLine("- resetblockchain");
            Console.WriteLine("- transfertokens");
            Console.WriteLine("What would you like to do:");
            Console.Write("> ");
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
