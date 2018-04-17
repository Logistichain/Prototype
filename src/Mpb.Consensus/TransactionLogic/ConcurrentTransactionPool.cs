using Mpb.Consensus.Exceptions;
using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.TransactionLogic
{
    public class ConcurrentTransactionPool
    {
        private readonly ITransactionValidator _transactionValidator;
        private List<AbstractTransaction> _txPool;

        public ConcurrentTransactionPool(ITransactionValidator transactionValidator)
        {
            _txPool = new List<AbstractTransaction>();
            _transactionValidator = transactionValidator;
        }

        public List<AbstractTransaction> Transactions => _txPool;

        // todo support logging framework
        public void AddTransaction(AbstractTransaction tx)
        {
            //_logger.Information("Miner received transaction: {0}", JsonConvert.SerializeObject(tx));
            try
            {
                if (_txPool.Contains(tx))
                {
                    throw new TransactionRejectedException("Transaction already submitted to txpool");
                }

                lock (_txPool)
                {
                    _transactionValidator.ValidateTransaction(tx);
                    _txPool.Add(tx);
                }
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
        }
    }
}
