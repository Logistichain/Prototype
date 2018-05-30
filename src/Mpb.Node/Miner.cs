using Mpb.Consensus.BlockLogic;
using Mpb.DAL;
using System;
using System.Threading;
using System.Threading.Tasks;
using Mpb.Model;
using System.Linq;
using System.Collections.Generic;
using Mpb.Consensus.TransactionLogic;
using Newtonsoft.Json;
using Mpb.Consensus.Exceptions;
using Microsoft.Extensions.Logging;
using Mpb.Shared;
using Mpb.Shared.Events;
using Mpb.Shared.Constants;

namespace Mpb.Node
{
    /// <summary>
    /// Async mining
    /// </summary>
    public class Miner
    {
        ILogger _logger;
        IBlockchainRepository _blockchainRepo;
        ITransactionRepository _transactionRepo;
        ITransactionCreator _transactionCreator;
        ITransactionValidator _transactionValidator;
        IDifficultyCalculator _difficultyCalculator;
        IPowBlockCreator _blockCreator;
        private readonly IBlockValidator _blockValidator;
        CancellationTokenSource _miningCancellationToken;
        Task _miningTask;
        Blockchain _blockchain;
        ConcurrentTransactionPool _txPool;
        string _networkIdentifier;
        string _walletPubKey;
        string _walletPrivKey;

        BigDecimal difficulty;
        int maxTransactionsPerBlock = BlockchainConstants.MaximumTransactionPerBlock;

        public Miner(string netId, string minerWalletPubKey, string minerWalletPrivKey,
                    IBlockchainRepository blockchainRepo, ITransactionRepository transactionRepo,
                    ITransactionCreator transactionCreator, ITransactionValidator transactionValidator,
                    IDifficultyCalculator difficultyCalculator, IPowBlockCreator blockCreator,
                    IBlockValidator blockValidator, ConcurrentTransactionPool txPool, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Miner>();
            _walletPubKey = minerWalletPubKey;
            _walletPrivKey = minerWalletPrivKey;
            _blockchainRepo = blockchainRepo;
            _networkIdentifier = netId;
            _blockchain = _blockchainRepo.GetChainByNetId(_networkIdentifier);
            _transactionRepo = transactionRepo;
            _transactionCreator = transactionCreator;
            _transactionValidator = transactionValidator;
            _difficultyCalculator = difficultyCalculator;
            _blockCreator = blockCreator;
            _blockValidator = blockValidator;
            _txPool = txPool;

            EventPublisher.GetInstance().OnUnvalidatedTransactionReceived += OnUnvalidatedTransactionReceived;
            EventPublisher.GetInstance().OnUnvalidatedBlockCreated += OnUnvalidatedBlockCreated;
            difficulty = _difficultyCalculator.CalculateCurrentDifficulty(_blockchain);
        }

        public bool IsMining => _miningTask != null && _miningTask.Status == TaskStatus.Running;

        private bool OnUnvalidatedBlockCreated(object sender, BlockCreatedEventArgs ev)
        {
            if (ev.Block.Header.MagicNumber != _networkIdentifier) return false;
            var blockExists = true;

            if (sender != this)
            {
                try
                {
                    _blockchainRepo.GetBlockByHash(ev.Block.Header.Hash, ev.Block.Header.MagicNumber);
                }
                catch (KeyNotFoundException)
                {
                    blockExists = false;
                }
            }

            if (!blockExists)
            {
                CheckForDifficultyUpdate();
                var target = BlockchainConstants.MaximumTarget / difficulty;
                try
                {
                    _blockValidator.ValidateBlock(ev.Block, target, _blockchain, true, true);
                    _logger.LogInformation("Received block from remote node");
                    _logger.LogDebug("Current height: {0}", _blockchain.CurrentHeight);
                    // Do not restart the task because that's buggy (it stops, but doesn't start)

                    foreach (var tx in ev.Block.Transactions)
                    {
                        _txPool.RemoveTransaction(tx);
                    }
                }
                catch (BlockRejectedException ex)
                {
                    _logger.LogInformation("Block with hash {0} was rejected: {1}", ex.Block.Header.Hash, ex.Message);
                    return false;
                }
                catch (TransactionRejectedException ex)
                {
                    _logger.LogInformation("Block with transaction hash {0} was rejected: {1}", ex.Transaction.Hash, ex.Message);
                    return false;
                }
            }
            return true;
        }

        private bool OnUnvalidatedTransactionReceived(object sender, TransactionReceivedEventArgs eventHandler)
        {
            // todo ban spamming nodes somehow..
            return AddTransactionToPool(eventHandler.Transaction, true);
        }

        public ConcurrentTransactionPool TransactionPool => _txPool;

        public string NetworkIdentifier => _networkIdentifier;

        public bool AddTransactionToPool(AbstractTransaction tx, bool publishToNetwork)
        {
            //_logger.LogInformation("Miner received transaction: {0}", JsonConvert.SerializeObject(tx));
            try
            {
                if (_txPool.Contains(tx))
                {
                    throw new TransactionRejectedException("Transaction already submitted to txpool");
                }

                _transactionValidator.ValidateTransaction(tx);
                _txPool.AddTransaction(tx);
                _logger.LogInformation("Added transaction to txpool ({0})", tx.Hash);

                if (publishToNetwork) // todo move this to ConcurrentTransactionPool maybe?
                {
                    EventPublisher.GetInstance().PublishValidTransactionReceived(this, new TransactionReceivedEventArgs(tx));
                }
                return true;
            }
            catch (TransactionRejectedException e)
            {
                var errorTx = e.Transaction ?? tx;
                _logger.LogInformation("Transaction with hash {0} was rejected: {1}", errorTx.Hash, e.Message);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogInformation("An {0} occurred: {1}", e.GetType().Name, e.Message);
                return false;
            }
        }

        public void StartMining()
        {
            if (IsMining)
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
            if (!IsMining)
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
            _logger.LogInformation("We want to achieve a total of {0} seconds for each {1} blocks to be created.", (BlockchainConstants.SecondsPerBlockGoal * BlockchainConstants.DifficultyUpdateCycle), BlockchainConstants.DifficultyUpdateCycle);
            _logger.LogInformation("Mining for blocks..");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Every x blocks, recalculate the difficulty and save the blockchain.
                CheckForDifficultyUpdate();
                _logger.LogDebug("Current height: {0}", _blockchain.CurrentHeight);

                // Calculate our current balance
                var allReceivedTransactions = _transactionRepo.GetAllReceivedByPublicKey(_walletPubKey, _networkIdentifier);
                ulong balance = _transactionRepo.GetTokenBalanceForPubKey(_walletPubKey, _networkIdentifier);
                _logger.LogDebug("Our balance: {0}", balance);

                // Create & add the coinbase transaction and then mine the block
                var coinbaseTx = _transactionCreator.CreateCoinBaseTransaction(_walletPubKey, _walletPrivKey, "Mined by Montapacking!");
                var transactions = new List<AbstractTransaction>() { coinbaseTx };
                lock (_txPool)
                {
                    int transactionsIncludedInBlock = _txPool.Count();
                    transactions.AddRange(_txPool.GetTransactions(maxTransactionsPerBlock - 1));
                    _logger.LogDebug("Inserted {0} transactions from the txpool into this block", transactions.Count - 1);
                }

                try
                {
                    if (difficulty < 1) { difficulty = 1; }
                    var newBlock = _blockCreator.CreateValidBlockAndAddToChain(_walletPrivKey, _blockchain, transactions, difficulty, cancellationToken);

                    lock (_txPool)
                    {
                        foreach (var transaction in newBlock.Transactions)
                        {
                            _txPool.RemoveTransaction(transaction);
                        }
                    }

                    _logger.LogInformation("Created a new block!");

                    EventPublisher.GetInstance().PublishValidatedBlockCreated(this, new BlockCreatedEventArgs(newBlock));
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Mining operation canceled.");
                }
                catch (BlockRejectedException ex)
                {
                    _logger.LogDebug("Our own block does not pass validation: {0}", ex.Message);
                }
                catch (NonceLimitReachedException)
                {
                    _logger.LogWarning("Nonce limit reached.");
                }
            }
        }

        private void CheckForDifficultyUpdate()
        {
            if (_blockchain.CurrentHeight % BlockchainConstants.DifficultyUpdateCycle == 0 && _blockchain.CurrentHeight > 0)
            {
                difficulty = _difficultyCalculator.CalculateCurrentDifficulty(_blockchain);
                _blockchainRepo.Update(_blockchain);
                _logger.LogInformation("Blockchain persisted.");
                var difficultyInfo = _difficultyCalculator.GetPreviousDifficultyUpdateInformation(_blockchain);
                _logger.LogInformation("Total time to create blocks {0}-{1}: {2} sec", difficultyInfo.BeginHeight, difficultyInfo.EndHeight - 1, difficultyInfo.TotalSecondsForBlocks);
            }
        }
    }
}
