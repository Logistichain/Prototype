﻿using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Logic.MiscLogic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Mpb.Consensus.Model;
using System.Linq;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using Mpb.Consensus.Logic.TransactionLogic;
using Newtonsoft.Json;
using Mpb.Consensus.Logic.Exceptions;
using Microsoft.Extensions.Logging;

namespace Mpb.Consensus.PoC
{
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
        ISkuRepository _skuRepo;
        List<AbstractTransaction> _txPool;
        ILogger _logger;
        string _walletPubKey;
        string _walletPrivKey;
        Task _miningTask;
        CancellationTokenSource _miningCancellationToken;

        public Miner(Blockchain blockchain, string minerWalletPubKey, string minerWalletPrivKey, ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<Miner>();
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
            _skuRepo = new SkuStateTxLocalFileRepository(_blockchainRepo, _transactionRepo);
            _transactionValidator = new StateTransactionValidator(_transactionByteConverter, _blockchainRepo, _transactionRepo, _skuRepo);
            _validator = new PowBlockValidator(_blockHeaderHelper, _transactionValidator, _timestamper);
            _difficultyCalculator = new DifficultyCalculator();
            _blockCreator = new PowBlockCreator(_timestamper, _validator, _blockHeaderHelper);
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
            BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
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
                    var newBlock = _blockCreator.CreateValidBlock(transactions, difficulty, cancellationToken);

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
            }
        }
    }
}
