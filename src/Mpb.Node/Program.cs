using Mpb.Consensus.BlockLogic;
using Mpb.DAL;
using System;
using Mpb.Model;
using Mpb.Consensus.TransactionLogic;
using Mpb.Node.Handlers;
using Mpb.Consensus.MiscLogic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Mpb.Node
{
    public class Program
    {
        private static Microsoft.Extensions.Logging.ILogger _logger;

        public static void Main(string[] args)
        {
            var walletPubKey = "montaminer";
            var walletPrivKey = "montaprivatekey";
            var networkIdentifier = "testnet";
            var services = SetupDI(networkIdentifier, walletPubKey, walletPrivKey);
            
            GetServices(
                services,
                out IBlockchainRepository blockchainRepo,
                out ITransactionRepository transactionRepo,
                out ITransactionCreator transactionCreator,
                out ITimestamper timestamper,
                out ISkuRepository skuRepository,
                out Miner miner
                );
            _logger = services.GetService<ILoggerFactory>().CreateLogger<Program>();
            Blockchain blockchain = blockchainRepo.GetChainByNetId(networkIdentifier);

            // Command handlers, only large commands are handles by these separate handlers.
            AccountsCommandHandler accountsCmdHandler = new AccountsCommandHandler(transactionRepo, networkIdentifier);
            SkusCommandHandler skusCmdHandler = new SkusCommandHandler(blockchainRepo, timestamper, skuRepository, networkIdentifier);
            TransactionsCommandHandler transactionsCmdHandler = new TransactionsCommandHandler(transactionRepo, networkIdentifier);
            TransactionPoolCommandHandler txpoolCmdHandler = new TransactionPoolCommandHandler();
            TransferTokensCommandHandler transferTokensCmdHandler = new TransferTokensCommandHandler(transactionRepo, transactionCreator);
            CreateSkuCommandHandler createSkuCmdHandler = new CreateSkuCommandHandler(transactionRepo, transactionCreator);
            TransferSupplyCommandHandler transferSupplyCmdHandler = new TransferSupplyCommandHandler(skuRepository, transactionRepo, transactionCreator, networkIdentifier);

            _logger.LogInformation("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
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
                        services = SetupDI(networkIdentifier, walletPubKey, walletPrivKey);
                        GetServices(
                            services,
                            out blockchainRepo,
                            out transactionRepo,
                            out transactionCreator,
                            out timestamper,
                            out skuRepository,
                            out miner
                        );
                        accountsCmdHandler = new AccountsCommandHandler(transactionRepo, networkIdentifier);
                        skusCmdHandler = new SkusCommandHandler(blockchainRepo, timestamper, skuRepository, networkIdentifier);
                        transactionsCmdHandler = new TransactionsCommandHandler(transactionRepo, networkIdentifier);
                        txpoolCmdHandler = new TransactionPoolCommandHandler();
                        transferTokensCmdHandler = new TransferTokensCommandHandler(transactionRepo, transactionCreator);
                        createSkuCmdHandler = new CreateSkuCommandHandler(transactionRepo, transactionCreator);
                        _logger.LogInformation("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
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

        private static void GetServices(IServiceProvider services, out IBlockchainRepository blockchainRepo,
                                        out ITransactionRepository transactionRepo, out ITransactionCreator transactionCreator,
                                        out ITimestamper timestamper, out ISkuRepository skuRepository, out Miner miner)
        {
            blockchainRepo = services.GetService<IBlockchainRepository>();
            transactionRepo = services.GetService<ITransactionRepository>();
            transactionCreator = services.GetService<ITransactionCreator>();
            timestamper = services.GetService<ITimestamper>();
            skuRepository = services.GetService<ISkuRepository>();
            miner = services.GetService<Miner>();
        }

        private static IServiceProvider SetupDI(string networkIdentifier, string walletPubKey, string walletPrivKey)
        {
            var blockchainRepo = new BlockchainLocalFileRepository();
            var services = new ServiceCollection()
                .AddSingleton(CreateLoggerFactory())
                .AddTransient<IBlockHeaderHelper, BlockHeaderHelper>()
                .AddTransient<IBlockValidator, PowBlockValidator>()
                .AddTransient<IDifficultyCalculator, DifficultyCalculator>()
                .AddTransient<IPowBlockCreator, PowBlockCreator>()

                .AddTransient<IBlockchainRepository, BlockchainLocalFileRepository>()
                .AddTransient<ISkuRepository, SkuStateTxLocalFileRepository>()
                .AddTransient<ITransactionRepository>(
                        (x) => new StateTransactionLocalFileRepository(x.GetService<IBlockchainRepository>().GetChainByNetId(networkIdentifier))
                    )
                .AddTransient<ITimestamper, UnixTimestamper>()

                .AddTransient<ITransactionCreator, StateTransactionCreator>()
                .AddTransient<ITransactionValidator, StateTransactionValidator>()
                .AddTransient<ITransactionFinalizer, StateTransactionFinalizer>()

                .AddTransient(
                        (x) => new Miner(
                            networkIdentifier, walletPubKey, walletPrivKey,
                            x.GetService<IBlockchainRepository>(),
                            x.GetService<ITransactionRepository>(), x.GetService<ITransactionCreator>(),
                            x.GetService<ITransactionValidator>(), x.GetService<IDifficultyCalculator>(),
                            x.GetService<IPowBlockCreator>(), x.GetService<ILoggerFactory>())
                    )

                .BuildServiceProvider();

            return services;
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

        private static ILoggerFactory CreateLoggerFactory()
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = new LoggerFactory()
                .AddSerilog(logger);

            return loggerFactory;
        }
    }
}
