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
        CancellationTokenSource _miningCancellationToken;
        Task _miningTask;
        Blockchain _blockchain;
        List<AbstractTransaction> _txPool;
        string _networkIdentifier;
        string _walletPubKey;
        string _walletPrivKey;

        public Miner(string netId, string minerWalletPubKey, string minerWalletPrivKey,
                    IBlockchainRepository blockchainRepo, ITransactionRepository transactionRepo,
                    ITransactionCreator transactionCreator, ITransactionValidator transactionValidator,
                    IDifficultyCalculator difficultyCalculator, IPowBlockCreator blockCreator,
                    ILoggerFactory loggerFactory)
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
            _txPool = new List<AbstractTransaction>();
        }

        public List<AbstractTransaction> TransactionPool => _txPool;

        public string NetworkIdentifier => _networkIdentifier;

        public void AddTransactionToPool(AbstractTransaction tx)
        {
            _logger.LogInformation("Miner received transaction: {0}", JsonConvert.SerializeObject(tx));
            try
            {
                if (_txPool.Contains(tx))
                {
                    throw new TransactionRejectedException("Transaction already submitted to txpool");
                }

                _transactionValidator.ValidateTransaction(tx);
                _txPool.Add(tx);
                _logger.LogInformation("Added transaction to txpool ({0})", tx.Hash);
            }
            catch (TransactionRejectedException e)
            {
                _logger.LogInformation("Transaction with hash {0} was rejected: {1}", e.Transaction.Hash, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogInformation("An {0} occurred: {1}", e.GetType().Name, e.Message);
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
            BigDecimal difficulty = 1;
            uint secondsPerBlockGoal = 3;
            var difficultyUpdateCycle = 5;

            _logger.LogInformation("We want to achieve a total of {0} seconds for each {1} blocks to be created.", (secondsPerBlockGoal * difficultyUpdateCycle), difficultyUpdateCycle);
            _logger.LogInformation("Mining for blocks..");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Every 10 blocks, recalculate the difficulty and save the blockchain.
                if (_blockchain.CurrentHeight % difficultyUpdateCycle == 0 && _blockchain.CurrentHeight > 0)
                {
                    difficulty = _difficultyCalculator.CalculateDifficulty(_blockchain, _blockchain.CurrentHeight, 1, secondsPerBlockGoal, difficultyUpdateCycle);
                    _blockchainRepo.Update(_blockchain);
                    _logger.LogInformation("Blockchain persisted.");
                    var difficultyInfo = _difficultyCalculator.GetPreviousDifficultyUpdateInformation(_blockchain, difficultyUpdateCycle);
                    _logger.LogInformation("Total time to create blocks {0}-{1}: {2} sec", difficultyInfo.BeginHeight, difficultyInfo.EndHeight - 1, difficultyInfo.TotalSecondsForBlocks);
                    _logger.LogDebug("Difficulty for next block {0}", difficulty);
                    _logger.LogDebug("Target for next block {0}", difficulty);
                }
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
                    for (int i = 0; i < _txPool.Count(); i++)
                    {
                        transactions.Add(_txPool[i]);
                    }

                    transactionsIncludedInBlock = transactionsIncludedInBlock - _txPool.Count();
                    _logger.LogDebug("Inserted {0} transactions from the txpool into this block", transactionsIncludedInBlock);
                }

                try
                {
                    if (difficulty < 1) { difficulty = 1; }
                    var newBlock = _blockCreator.CreateValidBlockAndAddToChain(_walletPrivKey, _blockchain, transactions, difficulty, cancellationToken);

                    lock (_blockchain)
                    {
                        _blockchain.Blocks.Add(newBlock);
                    }

                    lock (_txPool)
                    {
                        foreach(var transaction in newBlock.Transactions)
                        {
                            _txPool.Remove(transaction);
                        }
                    }

                    _logger.LogInformation("Created a new block!");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Mining operation canceled.");
                }
                catch(BlockRejectedException ex)
                {
                    _logger.LogWarning("Our own block does not pass validation: {0}", ex.Message);
                }
                catch(NonceLimitReachedException)
                {
                    _logger.LogWarning("Nonce limit reached.");
                }
            }
        }
    }
}
