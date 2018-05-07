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
using Mpb.Networking;
using System.Net;
using Mpb.Networking.Model;
using Mpb.Networking.Constants;

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
            ushort listeningPort = NetworkConstants.DefaultListeningPort;
            IPAddress publicIP = IPAddress.Parse("127.0.0.1"); // Our public IP so other nodes can find us, todo

            GetServices(
                services,
                out IBlockchainRepository blockchainRepo,
                out ITransactionRepository transactionRepo,
                out ITransactionCreator transactionCreator,
                out ITimestamper timestamper,
                out ISkuRepository skuRepository,
                out INetworkManager networkManager,
                out ILoggerFactory loggerFactory,
                out Miner miner
                );
            _logger = loggerFactory.CreateLogger<Program>();
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
            networkManager.AcceptConnections(publicIP, listeningPort, new System.Threading.CancellationTokenSource());

            networkManager.ConnectToPeer(new NetworkNode(ConnectionType.Outbound, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345)));

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
                        networkManager.Dispose();
                        _logger.LogWarning("All network connections shut down.");
                        // Initialize all variables again because the heap references changed.
                        services = SetupDI(networkIdentifier, walletPubKey, walletPrivKey);
                        GetServices(
                            services,
                            out blockchainRepo,
                            out transactionRepo,
                            out transactionCreator,
                            out timestamper,
                            out skuRepository,
                            out networkManager,
                            out var ingored,
                            out miner
                        );
                        networkManager.AcceptConnections(publicIP, listeningPort, new System.Threading.CancellationTokenSource()); // todo port constant
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
                    case "networking stop":
                        networkManager.Dispose();
                        break;
                    case "networking port":
                        Console.WriteLine("Specify the new listening port. Now it's " +listeningPort);
                        Console.Write("> ");
                        ushort newListeningPort = 0;
                        var portInput = Console.ReadLine().ToLower();
                        while (!ushort.TryParse(portInput, out newListeningPort) || newListeningPort > 65535)
                        {
                            Console.WriteLine("Invalid value. Use a positive numeric value without decimals. Maximum = 65535.");
                            Console.Write("> ");
                            portInput = Console.ReadLine().ToLower();
                        }
                        listeningPort = newListeningPort;
                        Console.WriteLine("Done. Restart the networking module to use the new port.");
                        Console.Write("> ");
                        break;
                    case "networking connect":
                        Console.WriteLine("Specify the IP to connect to (ip:port)");
                        Console.Write("> ");
                        var connPortInput = Console.ReadLine().ToLower();
                        try
                        {
                            string connectionIp = connPortInput.Split(':')[0];
                            int connectPort = ushort.Parse(connPortInput.Split(':')[1]);
                            networkManager.ConnectToPeer(new NetworkNode(ConnectionType.Outbound, new IPEndPoint(IPAddress.Parse(connectionIp), connectPort)));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Something went wrong. Command aborted.");
                        }
                        break;
                    case "networking start":
                        if (networkManager.IsDisposed)
                            networkManager = GetService<INetworkManager>(services);
                        networkManager.AcceptConnections(publicIP, listeningPort, new System.Threading.CancellationTokenSource());
                        break;
                    case "networking restart":
                        networkManager.Dispose();
                        networkManager = GetService<INetworkManager>(services);
                        networkManager.AcceptConnections(publicIP, listeningPort, new System.Threading.CancellationTokenSource());
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
                                        out ITimestamper timestamper, out ISkuRepository skuRepository,
                                        out INetworkManager networkManager, out ILoggerFactory loggerFactory, out Miner miner)
        {
            blockchainRepo = services.GetService<IBlockchainRepository>();
            transactionRepo = services.GetService<ITransactionRepository>();
            transactionCreator = services.GetService<ITransactionCreator>();
            timestamper = services.GetService<ITimestamper>();
            skuRepository = services.GetService<ISkuRepository>();
            networkManager = services.GetService<INetworkManager>();
            loggerFactory = services.GetService<ILoggerFactory>();
            miner = services.GetService<Miner>();
        }

        private static T GetService<T>(IServiceProvider services)
        {
            return services.GetService<T>();
        }


        private static IServiceProvider SetupDI(string networkIdentifier, string walletPubKey, string walletPrivKey)
        {
            var blockchainRepo = new BlockchainLocalFileRepository();
            var services = new ServiceCollection()
                .AddSingleton(CreateLoggerFactory())
                .AddTransient<IBlockFinalizer, PowBlockFinalizer>()
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
                .AddTransient<IMessageHandler, MessageHandler>()
                .AddTransient(x => ConcurrentTransactionPool.GetInstance().SetTransactionValidator(x.GetService<ITransactionValidator>()))
                .AddTransient(x => NetworkNodesPool.GetInstance(x.GetService<ILoggerFactory>()))
                .AddTransient<INetworkManager, NetworkManager>()

                .AddTransient(
                        (x) => new Miner(
                            networkIdentifier, walletPubKey, walletPrivKey,
                            x.GetService<IBlockchainRepository>(),
                            x.GetService<ITransactionRepository>(), x.GetService<ITransactionCreator>(),
                            x.GetService<ITransactionValidator>(), x.GetService<IDifficultyCalculator>(),
                            x.GetService<IPowBlockCreator>(), x.GetService<ConcurrentTransactionPool>(), 
                            x.GetService<ILoggerFactory>())
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
            Console.WriteLine("- networking start|stop|restart|port|connect");
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
