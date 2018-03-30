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
using Newtonsoft.Json;
using Mpb.Consensus.Logic.Exceptions;

namespace Mpb.Consensus.PoC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ILogger logger = CreateLogger();
            var networkIdentifier = "testnet";
            IBlockchainRepository blockchainRepo = new BlockchainLocalFileRepository();
            Blockchain blockchain = blockchainRepo.GetByNetId(networkIdentifier);
            ITransactionRepository transactionRepo = new StateTransactionLocalFileRepository(blockchain);
            ITransactionCreator transactionCreator = new StateTransactionCreator(new TransactionByteConverter());
            var walletPubKey = "montaminer";
            var walletPrivKey = "montaprivatekey";
            Miner miner = new Miner(blockchain, walletPubKey, walletPrivKey, logger);
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
                        var transactions = transactionRepo.GetAll(networkIdentifier).OfType<StateTransaction>();

                        if (transactions.Count() == 0)
                        {
                            Console.WriteLine("No transactions found.");
                        }
                        else
                        {
                            Console.WriteLine("Balances:");
                        }

                        var allAdresses = transactions.Select(tx => tx.ToPubKey).Distinct();
                        allAdresses = allAdresses.Concat(transactions.Select(tx => tx.FromPubKey).Distinct()).Where(address => address != null).Distinct();
                        foreach (var address in allAdresses)
                        {
                            var transactionCount = transactions.Where(tx => tx.ToPubKey == address || tx.FromPubKey == address).Count();
                            var addressBalance = GetBalanceForPubKey(address, networkIdentifier, transactionRepo);
                            Console.WriteLine("------- ACCOUNT -------");
                            Console.WriteLine("Public key: " + address);
                            Console.WriteLine("Transactions: " + transactionCount);
                            Console.WriteLine("Total balance: " + addressBalance);
                            Console.WriteLine("-------=========-------");
                        }
                        Console.Write("> ");
                        break;
                    case "txpool":
                    case "transactionpool":
                    case "pendingtransactions":
                        var allPendingTransactions = miner.TransactionPool.OfType<StateTransaction>();

                        if (allPendingTransactions.Count() == 0)
                        {
                            Console.WriteLine("No pending transactions.");
                        }
                        else
                        {
                            Console.WriteLine("Pending transactions:");
                        }

                        foreach (var transaction in allPendingTransactions)
                        {
                            Console.WriteLine("----- PENDING TRANSACTION -----");
                            Console.WriteLine("Hash: " + transaction.Hash);
                            Console.WriteLine("TxVersion: " + transaction.Version);
                            Console.WriteLine("From: " + transaction.FromPubKey ?? "<unknown>");
                            Console.WriteLine("To: " + transaction.ToPubKey ?? "<unknown>");
                            Console.WriteLine("Action: " + transaction.Action);
                            Console.WriteLine("Fee: " + transaction.Fee + " TK");
                            Console.WriteLine("Amount: " + transaction.Amount);
                            Console.WriteLine("SkuBlockHash: " + transaction.SkuBlockHash ?? "Not applicable");
                            Console.WriteLine("SkuTxIndex: " + transaction.SkuTxIndex ?? "Not applicable");
                            Console.WriteLine("Data: " + transaction.Data);
                            Console.WriteLine("-----=====================-----");
                        }
                        Console.Write("> ");
                        break;
                    case "transactions":
                        Console.WriteLine("Transactions:");
                        var allTransactions = transactionRepo.GetAll(networkIdentifier).OfType<StateTransaction>();
                        foreach(var transaction in allTransactions)
                        {
                            Console.WriteLine("----- TRANSACTION -----");
                            Console.WriteLine("Hash: " + transaction.Hash);
                            Console.WriteLine("TxVersion: " + transaction.Version);
                            Console.WriteLine("From: " + transaction.FromPubKey ?? "<unknown>");
                            Console.WriteLine("To: " + transaction.ToPubKey ?? "<unknown>");
                            Console.WriteLine("Action: " + transaction.Action);
                            Console.WriteLine("Fee: " + transaction.Fee + " TK");
                            Console.WriteLine("Amount: " + transaction.Amount);
                            Console.WriteLine("SkuBlockHash: " + transaction.SkuBlockHash ?? "Not applicable");
                            Console.WriteLine("SkuTxIndex: " + transaction.SkuTxIndex ?? "Not applicable");
                            Console.WriteLine("Data: " + transaction.Data);
                            Console.WriteLine("-----=============-----");
                        }
                        Console.Write("> ");
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
                        blockchain = blockchainRepo.GetByNetId(networkIdentifier);
                        transactionRepo = new StateTransactionLocalFileRepository(blockchain);
                        miner = new Miner(blockchain, walletPubKey, walletPrivKey, logger);
                        logger.Information("Loaded blockchain. Current height: {Height}", blockchain.CurrentHeight == -1 ? "GENESIS" : blockchain.CurrentHeight.ToString());
                        Console.Write("> ");
                        break;
                    case "transfertokens":
                        uint tokenFee = 10; // From BlockchainConstants.cs
                        Console.WriteLine("Current transfer token fee is " + tokenFee + " TK.");
                        WriteLineWithInputCursor("Enter the sender's public key:");
                        var fromPub = Console.ReadLine().ToLower();
                        var balance = GetBalanceForPubKey(fromPub, networkIdentifier, transactionRepo);

                        Console.WriteLine("The sender's balance: " + balance);
                        WriteLineWithInputCursor("Enter the sender's private key (can be anything for now):");
                        var fromPriv = Console.ReadLine().ToLower();
                        
                        WriteLineWithInputCursor("Enter the receiver's public key:");
                        var toPub = Console.ReadLine().ToLower();

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

                        uint amount = 0;
                        bool forceAmount = false;
                        while (amount < 1 || amount > balance && !forceAmount)
                        {
                            Console.WriteLine("Enter the amount to send:");

                            var amountInput = Console.ReadLine().ToLower();
                            while (!UInt32.TryParse(amountInput, out amount))
                            {
                                WriteLineWithInputCursor("Invalid value. Use a positive numeric value without decimals.");
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
                        var optionalData = Console.ReadLine().ToLower();

                        AbstractTransaction transactionToSend = transactionCreator.CreateTokenTransferTransaction(fromPub, fromPriv, toPub, amount, optionalData);
                        miner.AddTransactionToPool(transactionToSend);
                        Console.Write("> ");
                        break;
                    default:
                        WriteLineWithInputCursor("I don't recognize that command.");
                        break;
                }
            }
        }

        private static void WriteLineWithInputCursor(string writeLine)
        {
            Console.WriteLine(writeLine);
            Console.Write("> ");
        }

        private static void PrintConsoleCommands()
        {
            Console.WriteLine("----- [MontaBlockchain] -----");
            Console.WriteLine("Available commands:");
            Console.WriteLine("- help");
            Console.WriteLine("- transactions");
            Console.WriteLine("- txpool / transactionpool / pendingtransactions");
            Console.WriteLine("- accounts / users / balances");
            Console.WriteLine("- startmining");
            Console.WriteLine("- stopmining");
            Console.WriteLine("- resetblockchain");
            Console.WriteLine("- transfertokens");
            WriteLineWithInputCursor("What would you like to do:");
        }

        public static long GetBalanceForPubKey(string pubKey, string netId, ITransactionRepository transactionRepo)
        {
            long totalReceived = 0;
            long totalSpent = 0;
            var allTransactions = transactionRepo.GetAllByPublicKey(pubKey, netId).OfType<StateTransaction>().ToList();
            foreach (StateTransaction tx in allTransactions)
            {
                if (tx.FromPubKey == pubKey)
                {
                    totalSpent += tx.Amount + tx.Fee;
                }
               
                if (tx.ToPubKey == pubKey)
                {
                    totalReceived += tx.Amount;
                }
            }

            return totalReceived - totalSpent;
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

    /// <summary>
    /// Async mining
    /// </summary>
    public class Miner
    {
        IBlockchainRepository _blockchainRepo;
        string _networkIdentifier;
        Blockchain _blockchain;
        ITransactionRepository _transactionRepo;
        IBlockHeaderHelper _blockHeaderHelper;
        ITimestamper _timestamper;
        TransactionByteConverter _transactionByteConverter;
        ITransactionCreator _transactionCreator;
        ITransactionValidator _transactionValidator;
        IBlockValidator _validator;
        IDifficultyCalculator _difficultyCalculator;
        IPowBlockCreator _blockCreator;
        List<AbstractTransaction> _txPool;
        ILogger _logger;
        string _walletPubKey;
        string _walletPrivKey;
        Task _miningTask;
        CancellationTokenSource _miningCancellationToken;

        public Miner(Blockchain blockchain, string minerWalletPubKey, string minerWalletPrivKey, ILogger logger)
        {
            _logger = logger;
            _walletPubKey = minerWalletPubKey;
            _walletPrivKey = minerWalletPrivKey;
            _blockchainRepo = new BlockchainLocalFileRepository();
            _networkIdentifier = "testnet";
            _blockchain = blockchain;
            _transactionRepo = new StateTransactionLocalFileRepository(blockchain);
            _blockHeaderHelper = new BlockHeaderHelper();
            _timestamper = new UnixTimestamper();
            _transactionByteConverter = new TransactionByteConverter();
            _transactionCreator = new StateTransactionCreator(_transactionByteConverter);
            _transactionValidator = new StateTransactionValidator(_transactionByteConverter, _transactionRepo);
            _validator = new PowBlockValidator(_blockHeaderHelper, _transactionValidator, _timestamper);
            _difficultyCalculator = new DifficultyCalculator();
            _blockCreator = new PowBlockCreator(_timestamper, _validator, _blockHeaderHelper);
            _txPool = new List<AbstractTransaction>();
        }

        public List<AbstractTransaction> TransactionPool => _txPool;

        public void AddTransactionToPool(AbstractTransaction tx)
        {
            _logger.Information("Miner received transaction: {0}", JsonConvert.SerializeObject(tx));
            try
            {
                if (_txPool.Contains(tx))
                {
                    throw new TransactionRejectedException("Transaction already submitted to txpool");
                }

                _transactionValidator.ValidateTransaction(tx);
                _txPool.Add(tx);
                _logger.Information("Added transaction to txpool ({0})", tx.Hash);
            }
            catch (TransactionRejectedException e)
            {
                _logger.Information("Transaction with hash {0} was rejected: {1}", e.Transaction.Hash, e.Message);
            }
            catch (Exception e)
            {
                _logger.Information("An {0} occurred: {1}", e.GetType().Name, e.Message);
            }
        }

        public void StartMining()
        {
            if (_miningTask != null && _miningTask.Status == TaskStatus.Running)
            {
                Console.WriteLine("Already mining.");
            }
            else
            {
                _miningCancellationToken = new CancellationTokenSource();
                _miningTask = Task.Run(() => MineForBlocks(_miningCancellationToken.Token), _miningCancellationToken.Token);
            }
        }

        public void StopMining(bool writeMessagesToConsole)
        {
            if (_miningTask == null || _miningCancellationToken == null || (_miningTask != null && _miningTask.Status != TaskStatus.Running))
            {
                if (writeMessagesToConsole)
                {
                    Console.WriteLine("Mining is not active right now.");
                }
            }
            else
            {
                _miningCancellationToken.Cancel();
            }
        }

        private void MineForBlocks(CancellationToken cancellationToken)
        {
            BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            BigDecimal difficulty = 1;
            uint secondsPerBlockGoal = 3;
            var difficultyUpdateCycle = 5;

            _logger.Information("We want to achieve a total of {0} seconds for each {1} blocks to be created.", (secondsPerBlockGoal * difficultyUpdateCycle), difficultyUpdateCycle);
            _logger.Information("Mining for blocks..");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Every 10 blocks, recalculate the difficulty and save the blockchain.
                if (_blockchain.CurrentHeight % difficultyUpdateCycle == 0 && _blockchain.CurrentHeight > 0)
                {
                    difficulty = _difficultyCalculator.CalculateDifficulty(_blockchain, _blockchain.CurrentHeight, 1, secondsPerBlockGoal, difficultyUpdateCycle);
                    _blockchainRepo.Update(_blockchain);
                    _logger.Information("Blockchain persisted.");
                    var difficultyInfo = _difficultyCalculator.GetPreviousDifficultyUpdateInformation(_blockchain, difficultyUpdateCycle);
                    _logger.Information("Total time to create blocks {0}-{1}: {2} sec", difficultyInfo.BeginHeight, difficultyInfo.EndHeight - 1, difficultyInfo.TotalSecondsForBlocks);
                    _logger.Debug("Difficulty for next block {0}", difficulty);
                    _logger.Debug("Target for next block {0}", difficulty);
                }
                _logger.Debug("Current height: {0}", _blockchain.CurrentHeight);

                // Calculate our current balance
                var allReceivedTransactions = _transactionRepo.GetAllReceivedByPublicKey(_walletPubKey, _networkIdentifier);
                long balance = Program.GetBalanceForPubKey(_walletPubKey, _networkIdentifier, _transactionRepo);
                _logger.Debug("Our balance: {0}", balance);

                // Create & add the coinbase transaction and then mine the block
                var coinbaseTx = _transactionCreator.CreateCoinBaseTransaction(_walletPubKey, _walletPrivKey, "Mined by Montapacking!");
                var transactions = new List<AbstractTransaction>() { coinbaseTx };
                lock (_txPool)
                {
                    int transactionsIncludedInBlock = _txPool.Count();
                    for (int i = 0; i < _txPool.Count(); i++)
                    {
                        transactions.Add(_txPool[i]);
                        _txPool.RemoveAt(i);
                    }

                    transactionsIncludedInBlock = transactionsIncludedInBlock - _txPool.Count();
                    _logger.Debug("Inserted {0} transactions from the txpool into this block", transactionsIncludedInBlock);
                }

                try
                {
                    var newBlock = _blockCreator.CreateValidBlock(transactions, difficulty, cancellationToken);

                    lock (_blockchain)
                    {
                        _blockchain.Blocks.Add(newBlock);
                    }
                    _logger.Information("Found a new block!");
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Mining operation canceled.");
                }
            }
        }
    }
}
