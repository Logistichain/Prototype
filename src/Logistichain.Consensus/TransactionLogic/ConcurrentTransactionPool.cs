using Logistichain.Consensus.Exceptions;
using Logistichain.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logistichain.Consensus.TransactionLogic
{
    public sealed class ConcurrentTransactionPool
    {
        private ITransactionValidator _transactionValidator;
        private ConcurrentDictionary<string, AbstractTransaction> _txPool;
        private static volatile ConcurrentTransactionPool _instance;
        private static object _threadLock = new Object();

        private ConcurrentTransactionPool()
        {
            _txPool = new ConcurrentDictionary<string, AbstractTransaction>();
        }

        /// <summary>
        /// Gets the singleton instance for this object.
        /// </summary>
        /// <returns></returns>
        public static ConcurrentTransactionPool GetInstance()
        {
            if (_instance == null)
            {
                lock (_threadLock)
                {
                    if (_instance == null)
                        _instance = new ConcurrentTransactionPool();
                }
            }

            return _instance;
        }

        /// <summary>
        /// Don't forget to set the transaction validator after instantiating the transactionpool.
        /// The dependency injector is responsible for this call.
        /// </summary>
        /// <param name="transactionValidator"></param>
        /// <returns>This instance (fluent)</returns>
        public ConcurrentTransactionPool SetTransactionValidator(ITransactionValidator transactionValidator)
        {
            _transactionValidator = transactionValidator;
            return this;
        }

        // todo support logging framework
        /// <summary>
        /// Adds a transaction to the transactionpool.
        /// </summary>
        /// <param name="tx"></param>
        public bool AddTransaction(AbstractTransaction tx)
        {
            //_logger.Information("Miner received transaction: {0}", JsonConvert.SerializeObject(tx));
            try
            {
                if (_txPool.Keys.Contains(tx.Hash))
                {
                    throw new TransactionRejectedException("Transaction already submitted to txpool");
                }

                _transactionValidator.ValidateTransaction(tx);
                return _txPool.TryAdd(tx.Hash, tx);
                //_logger.Information("Added transaction to txpool ({0})", tx.Hash);
            }
            catch (TransactionRejectedException)
            {
                //_logger.Information("Transaction with hash {0} was rejected: {1}", e.Transaction.Hash, e.Message);
            }
            catch (Exception)
            {
                //_logger.Information("An {0} occurred: {1}", e.GetType().Name, e.Message);
            }

            return false;
        }

        /// <summary>
        /// Tries to get the amount of transactions and return them.
        /// </summary>
        /// <param name="maxAmount">The maximum amount of transactions to take</param>
        /// <returns>Array of transactions. This can contain less (or no) transactions than the maxAmount.</returns>
        public IEnumerable<AbstractTransaction> GetTransactions(int maxAmount)
        {
            if (maxAmount < 1)
            {
                throw new ArgumentOutOfRangeException("Maximum amount must be higher than 0");
            }

            return _txPool.Values.Take(maxAmount > _txPool.Values.Count ? _txPool.Values.Count : maxAmount);
        }
        

        /// <summary>
        /// Tries to get the amount of transactions and return them.
        /// </summary>
        /// <returns>Array of transactions</returns>
        public IEnumerable<AbstractTransaction> GetAllTransactions()
        {
            return _txPool.Values.Take(_txPool.Values.Count);
        }

        public bool RemoveTransaction(AbstractTransaction tx)
        {
            return _txPool.TryRemove(tx.Hash, out var ignored);
        }

        public int Count() => _txPool.Count;
        public bool Contains(AbstractTransaction tx) => _txPool.Contains(new KeyValuePair<string, AbstractTransaction>(tx.Hash, tx));
    }
}
