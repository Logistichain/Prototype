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
using System.Threading;
using Mpb.Consensus.Cryptography;
using Mpb.Shared.Events;
using Mpb.Shared.Constants;
using System.Collections.Generic;

namespace Mpb.Node
{
    public class Program
    {
        private static Microsoft.Extensions.Logging.ILogger _logger;

        public static void Main(string[] args)
        {
            CryptographyCommandhandler cryptographyCmdHandler = new CryptographyCommandhandler(new KeyGenerator());
            string walletPubKey = "";
            string walletPrivKey = "";
            if (args.Length > 1 && args[0] == "-key" && args[1] == "1")
            {
                walletPubKey = "Ndwd8RhQTCuP3i7zxoiZboScLYVto1kZsTKZu3ejzQpwzw7tpCohpNCwXSaRxDjUtTpyC1pgjTeNXDEZ5qeEmXSU";
                walletPrivKey = "2a5a9964e175b551ee60621611f07cb45d10157334aa0377f58d9b6927b7949bc9985294e7183bb38fdac0de68098554f0102079d6c2657f57216e54d94b2a36cbc64d799ee7cd44d561e45aea240fec07467916961ac50632d23dc6aae6da43b08912a1bc0cb60943dba475de0b953211814d92d2139fb1014646cbaa069aee87a0aa60116e30fa906fd3bb764f27273c0ab4289947709ce9619f75c6ea5e3c40b6ee3e603420a3515201633fe43bb11e1e8e9f5172543f2f0d76b0ceba34a610977abf61e9c708e911d646450d5c0526ee56a75bbfeace61ae0f53bd70d4971e5b52a8697b50355786b53f9c0df921b0718ae0638c4b51acac5eb2da744a79";
            }
            else
            if (args.Length > 1 && args[0] == "-key" && args[1] == "2")
            {
                walletPubKey = "P86FGHAHaeoPiaJpysH1ahJMtfTg6jLFsHpaeN4RMyXFXioayL26PjXcpNiyzc3r7Xpn8mx1MUgSsRQXbM3TfY9u";
                walletPrivKey = "553c57ea38f82bdd0cd2dff6308c09b1f6c7f9d6a9406e9a15474458182883b3e190de71940404f551be2fdde2ad59b4680452fc92508514cebcd4f1b4d3c5e44e2b14a0ad981df1c30485ea1190b868ed90f014d68721900bc9a2d73013417df94a8fb698b906cfe660398dcb9a033e046c0323e3b10ec60a8494e3f545c1afc308f0c60001d0348a202d754e93c570c664d3c30f1d99483cc8ac142422d9a19e89cac2a93d3d029cce99c384987876be241d9171fda0a56ea8893855bc1067468bd6c4c902c78927d1274183d6c4a45035550004482fa1e9245401b305acdfbe317cf8c1b3adbb5d946aa0d55e2198e3e5e81f661063fe5c35dba4aec5d521";
            }
            else
            {
                cryptographyCmdHandler.HandleGenerateKeysCommand(out walletPubKey, out walletPrivKey);
            }
            PushKeyPair(walletPubKey, walletPrivKey);

            Console.WriteLine("Your new public key: " + walletPubKey);
            Console.WriteLine("Your new private key: " + walletPrivKey);
            Console.WriteLine("Loading blockchain..");

            var networkIdentifier = "testnet";
            var services = SetupDI(networkIdentifier, walletPubKey, walletPrivKey);
            ushort listeningPort = NetworkConstants.DefaultListeningPort;
            IPAddress publicIP = IPAddress.Parse("127.0.0.1"); // Our public IP so other nodes can find us, todo

            if (args.Length > 1 && args[0] == "-port")
            {
                listeningPort = ushort.Parse(args[1]);
            }

            GetServices(
                services,
                out IBlockchainRepository blockchainRepo,
                out ITransactionRepository transactionRepo,
                out ITransactionCreator transactionCreator,
                out ITimestamper timestamper,
                out ISkuRepository skuRepository,
                out INetworkManager networkManager,
                out ILoggerFactory loggerFactory,
                out ISkuRepository skuRepo,
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
            NetworkingCommandHandler networkingCmdHandler = new NetworkingCommandHandler();
            TransactionGeneratorCommandHandler txGeneratorCmdHandler = new TransactionGeneratorCommandHandler(miner, transactionCreator, skuRepo, blockchainRepo);
            CreateSupplyCommandHandler createSupplyCmdHandler = new CreateSupplyCommandHandler(skuRepository, transactionRepo, transactionCreator, networkIdentifier);
            DestroySupplyCommandHandler destroySupplyCmdHandler = new DestroySupplyCommandHandler(skuRepository, transactionRepo, transactionCreator, networkIdentifier);

            _logger.LogInformation("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
            networkManager.AcceptConnections(publicIP, listeningPort, new CancellationTokenSource());

            networkManager.ConnectToPeer(new NetworkNode(ConnectionType.Outbound, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345)));

            PrintConsoleCommands();

            var skuTransactions = 0;
            var txpool = ConcurrentTransactionPool.GetInstance();
            EventPublisher.GetInstance().OnValidTransactionReceived += (object sender, TransactionReceivedEventArgs txargs) =>
            {
                if (txargs.Transaction.Action == TransactionAction.CreateSku.ToString())
                    skuTransactions++;

                if (skuTransactions > 200000 && txpool.Count() < 1)
                {
                    miner.StopMining(true);
                    txGeneratorCmdHandler.HandleStopCommand();
                }
            };

            var input = "";
            while (input != "exit")
            {
                input = Console.ReadLine().ToLower();
                switch (input)
                {
                    case "help":
                        PrintConsoleCommands();
                        break;
                    case "transactiongenerator startandmine":
                        txGeneratorCmdHandler.HandleStartCommand(true);
                        break;
                    case "transactiongenerator start":
                        txGeneratorCmdHandler.HandleStartCommand(false);
                        break;
                    case "transactiongenerator stop":
                        txGeneratorCmdHandler.HandleStopCommand();
                        break;
                    case "generatekeys":
                        cryptographyCmdHandler.HandleGenerateKeysCommand(out walletPubKey, out walletPrivKey);
                        PushKeyPair(walletPubKey, walletPrivKey);
                        Console.WriteLine("Your new public key: " + walletPubKey);
                        Console.WriteLine("Your new private key: " + walletPrivKey);
                        Console.Write("> ");
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
                        PrintConsoleCommands();
                        break;
                    case "resetblockchain":
                        miner.StopMining(false);
                        blockchainRepo.Delete(networkIdentifier);
                        Console.WriteLine("Blockchain deleted.");
                        blockchain = blockchainRepo.GetChainByNetId(networkIdentifier);
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
                            out skuRepo,
                            out miner
                        );
                        networkManager.AcceptConnections(publicIP, listeningPort, new CancellationTokenSource());
                        accountsCmdHandler = new AccountsCommandHandler(transactionRepo, networkIdentifier);
                        skusCmdHandler = new SkusCommandHandler(blockchainRepo, timestamper, skuRepository, networkIdentifier);
                        transactionsCmdHandler = new TransactionsCommandHandler(transactionRepo, networkIdentifier);
                        txpoolCmdHandler = new TransactionPoolCommandHandler();
                        transferTokensCmdHandler = new TransferTokensCommandHandler(transactionRepo, transactionCreator);
                        createSkuCmdHandler = new CreateSkuCommandHandler(transactionRepo, transactionCreator);
                        txGeneratorCmdHandler = new TransactionGeneratorCommandHandler(miner, transactionCreator, skuRepo, blockchainRepo);
                        _logger.LogInformation("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
                        Console.Write("> ");
                        break;
                    case "transfertokens":
                        transferTokensCmdHandler.HandleCommand(networkIdentifier);
                        break;
                    case "createsku":
                        createSkuCmdHandler.HandleCommand(networkIdentifier);
                        break;
                    case "transfersupply":
                        transferSupplyCmdHandler.HandleCommand();
                        break;
                    case "createsupply":
                        createSupplyCmdHandler.HandleCommand();
                        break;
                    case "destroysupply":
                        destroySupplyCmdHandler.HandleCommand();
                        break;
                    case "networking setport":
                        listeningPort = networkingCmdHandler.HandleSetPortCommand(listeningPort);
                        break;
                    case "networking setaddress":
                        publicIP = networkingCmdHandler.HandleSetAddressCommand(publicIP);
                        break;
                    case "networking connect":
                        networkingCmdHandler.HandleConnectCommand(networkManager);
                        break;
                    case "networking disconnect":
                        networkingCmdHandler.HandleDisconnectCommand(networkManager);
                        break;
                    case "networking pool":
                        networkingCmdHandler.HandleListPoolCommand(NetworkNodesPool.GetInstance(loggerFactory));
                        break;
                    case "networking stop":
                        networkManager.Dispose();
                        break;
                    case "networking start":
                        if (networkManager.IsDisposed)
                            networkManager = GetService<INetworkManager>(services);
                        networkManager.AcceptConnections(publicIP, listeningPort, new CancellationTokenSource());
                        break;
                    case "networking restart":
                        networkManager.Dispose();
                        Thread.Sleep(1000);
                        networkManager = GetService<INetworkManager>(services);
                        networkManager.AcceptConnections(publicIP, listeningPort, new CancellationTokenSource());
                        break;
                    default:
                        Console.WriteLine("I don't recognize that command.");
                        Console.Write("> ");
                        break;
                }
            }
        }

        // This keystore code was hacked together in order to save private keys properly.
        // The private keys are too long for user input so we need to save them when they are generated.
        private static Dictionary<string, string> keyStore = new Dictionary<string, string>();
        public static void PushKeyPair(string pubKey, string privKey)
        {
            keyStore.Add(pubKey, privKey);
        }

        public static string GetPrivKey(string pubKey)
        {
            keyStore.TryGetValue(pubKey, out string privKey);
            return privKey;
        }


        private static void GetServices(IServiceProvider services, out IBlockchainRepository blockchainRepo,
                                        out ITransactionRepository transactionRepo, out ITransactionCreator transactionCreator,
                                        out ITimestamper timestamper, out ISkuRepository skuRepository,
                                        out INetworkManager networkManager, out ILoggerFactory loggerFactory,
                                        out ISkuRepository skuRepo, out Miner miner)
        {
            blockchainRepo = services.GetService<IBlockchainRepository>();
            transactionRepo = services.GetService<ITransactionRepository>();
            transactionCreator = services.GetService<ITransactionCreator>();
            timestamper = services.GetService<ITimestamper>();
            skuRepository = services.GetService<ISkuRepository>();
            networkManager = services.GetService<INetworkManager>();
            loggerFactory = services.GetService<ILoggerFactory>();
            skuRepo = services.GetService<ISkuRepository>();
            miner = services.GetService<Miner>();
        }

        private static T GetService<T>(IServiceProvider services)
        {
            return services.GetService<T>();
        }


        private static IServiceProvider SetupDI(string networkIdentifier, string walletPubKey, string walletPrivKey)
        {
            var services = new ServiceCollection()
                .AddSingleton(CreateLoggerFactory())
                .AddTransient<IBlockFinalizer, PowBlockFinalizer>()
                .AddTransient<IBlockValidator, PowBlockValidator>()
                .AddTransient<IDifficultyCalculator, DifficultyCalculator>()
                .AddTransient<IPowBlockCreator, PowBlockCreator>()

                .AddSingleton<IBlockchainRepository, BlockchainLocalFileRepository>()
                .AddSingleton<ISkuRepository, SkuStateTxLocalFileRepository>()
                .AddSingleton<ITransactionRepository, StateTransactionLocalFileRepository>()
                .AddTransient<ITimestamper, UnixTimestamper>()


                .AddTransient<ISigner, Signer>()
                .AddTransient<IKeyGenerator, KeyGenerator>()

                .AddTransient<ITransactionCreator, StateTransactionCreator>()
                .AddTransient<ITransactionValidator, StateTransactionValidator>()
                .AddTransient<ITransactionFinalizer, StateTransactionFinalizer>()
                .AddTransient<AbstractMessageHandler, MessageHandler>()
                .AddTransient(x => ConcurrentTransactionPool.GetInstance().SetTransactionValidator(x.GetService<ITransactionValidator>()))
                .AddTransient(x => NetworkNodesPool.GetInstance(x.GetService<ILoggerFactory>()))
                .AddTransient<INetworkManager, NetworkManager>(
                        (x) => new NetworkManager(
                            x.GetService<NetworkNodesPool>(),
                            x.GetService<ILoggerFactory>(),
                            x.GetService<IBlockValidator>(),
                            x.GetService<IDifficultyCalculator>(),
                            x.GetService<IBlockchainRepository>(),
                            networkIdentifier)
                    )

                .AddTransient(
                        (x) => new Miner(
                            networkIdentifier, walletPubKey, walletPrivKey,
                            x.GetService<IBlockchainRepository>(),
                            x.GetService<ITransactionRepository>(), x.GetService<ITransactionCreator>(),
                            x.GetService<ITransactionValidator>(), x.GetService<IDifficultyCalculator>(),
                            x.GetService<IPowBlockCreator>(), x.GetService<IBlockValidator>(),
                            x.GetService<ConcurrentTransactionPool>(), x.GetService<ILoggerFactory>())
                    )

                .BuildServiceProvider();

            return services;
        }

        private static void PrintConsoleCommands()
        {
            Console.WriteLine("----- [MontaBlockchain] -----");
            Console.WriteLine("Available commands:");
            Console.WriteLine("- help");
            Console.WriteLine("- generatekeys");
            Console.WriteLine("- transactiongenerator startandmine|start|stop");
            Console.WriteLine("- transactions");
            Console.WriteLine("- txpool / transactionpool / pendingtransactions");
            Console.WriteLine("- accounts / users / balances");
            Console.WriteLine("- skus");
            Console.WriteLine("- createsku");
            Console.WriteLine("- createsupply");
            Console.WriteLine("- transfersupply");
            Console.WriteLine("- destroysupply");
            Console.WriteLine("- startmining");
            Console.WriteLine("- stopmining");
            Console.WriteLine("- resetblockchain");
            Console.WriteLine("- transfertokens");
            Console.WriteLine("- networking start|stop|restart|setaddress|setport|connect|disconnect|pool");
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
