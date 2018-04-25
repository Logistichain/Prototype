using Mpb.Consensus.BlockLogic;
using Mpb.DAL;
using Mpb.Consensus.Exceptions;
using Mpb.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mpb.Shared.Constants;

namespace Mpb.Consensus.TransactionLogic
{
    public class StateTransactionValidator : ITransactionValidator
    {
        private readonly ITransactionFinalizer _txFinalizer;
        private readonly IBlockchainRepository _blockchainRepository;
        private readonly ITransactionRepository _transactionRepo;
        private readonly ISkuRepository _skuRepo;

        public StateTransactionValidator(ITransactionFinalizer txFinalizer, IBlockchainRepository blockchainRepository, ITransactionRepository transactionRepo, ISkuRepository skuRepo)
        {
            _txFinalizer = txFinalizer;
            _blockchainRepository = blockchainRepository;
            _transactionRepo = transactionRepo;
            _skuRepo = skuRepo;
        }

        // Todo this is not state-specific. Place this in an abstract class?
        //! This method does not build a merkle tree, but just hashes every transaction and adds them together!
        // Todo Implement a proper merkle root algorithm
        /// <summary>
        /// This method creates the hash for the entire given transaction list.
        /// If something changes, like the order or even a single bit in a transaction,
        /// the output will be completely different. This method is used to 'seal' all
        /// transactions before signing them.
        /// NOTE: This method does not build a merkle tree, but just hashes every transaction and adds them together!
        /// </summary>
        /// <param name="orderedTransactions">The transactions, in the correct order</param>
        /// <returns>The SHA-256 value for the merkleroot</returns>
        public virtual string CalculateMerkleRoot(ICollection<AbstractTransaction> orderedTransactions)
        {
            var hashString = "";
            List<AbstractTransaction> transactionsToLoopThrough = orderedTransactions.ToList();
            if (transactionsToLoopThrough.Count % 2 != 0)
            {
                transactionsToLoopThrough.Add(transactionsToLoopThrough.Last());
            }

            List<byte[]> hashesList = new List<byte[]>();
            foreach (AbstractTransaction t in transactionsToLoopThrough)
            {
                hashesList.Add(_txFinalizer.GetTransactionBytes(t));
            }

            int merkleRootArraySize = 0;
            hashesList.ForEach(hashByteArr => merkleRootArraySize += hashByteArr.Length);
            byte[] merkleRoot = new byte[merkleRootArraySize];

            // Copy the bytes to array
            int loopedSize = 0;
            for (int i = 0; i < transactionsToLoopThrough.Count; i++)
            {
                if (hashesList[i].Length > 0)
                {
                    Buffer.BlockCopy(hashesList[i], 0, merkleRoot, loopedSize, hashesList[i].Length);
                    loopedSize += hashesList[i].Length;
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(merkleRoot);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }

            return hashString;
        }

        // Maybe we can solve the scalability of this class with a pattern: Visitor?
        // Also take into account the multiple transaction versions that may exist.
        public virtual void ValidateTransaction(AbstractTransaction tx)
        {
            ValidateTransaction(tx, BlockchainConstants.DefaultNetworkIdentifier);
        }


        public virtual void ValidateTransaction(AbstractTransaction tx, string netIdentifier)
        {
            if (!(tx is StateTransaction))
            {
                throw new ArgumentException("Transaction is not of type StateTransaction.");
            }

            var stateTx = (StateTransaction)tx;

            if (tx.Version != BlockchainConstants.TransactionVersion)
            {
                throw new TransactionRejectedException("Unsupported transaction version", tx);
            }

            CheckTxIsCorrectlyHashedAndSigned(stateTx);

            // Action-specific checks
            if (stateTx.Action == TransactionAction.TransferToken.ToString())
            {
                ValidateTransferTokenTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.TransferSupply.ToString())
            {
                ValidateTransferSupplyTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.DestroySupply.ToString())
            {
                ValidateDestroySupplyTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.CreateSupply.ToString())
            {
                ValidateCreateSupplyTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.CreateSku.ToString())
            {
                ValidateCreateSkuTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.ChangeSku.ToString())
            {
                ValidateChangeSkuTransaction(stateTx, netIdentifier);
            }
            else if (stateTx.Action == TransactionAction.ClaimCoinbase.ToString())
            {
                ValidateCoinbaseTransaction(stateTx, netIdentifier);
            }
            else
            {
                throw new TransactionRejectedException("Unrecognized action", stateTx);
            }
        }

        private void ValidateTransferTokenTransaction(StateTransaction tx, string netIdentifier)
        {
            CheckFromAndToNotNull(tx);
            CheckTransactionFee(tx, BlockchainConstants.TransferTokenFee);
            CheckTokenBalance(tx.FromPubKey, netIdentifier, tx.Amount + BlockchainConstants.TransferTokenFee);
        }

        private void ValidateTransferSupplyTransaction(StateTransaction tx, string netIdentifier)
        {
            CheckFromAndToNotNull(tx);

            if (String.IsNullOrWhiteSpace(tx.SkuBlockHash))
            {
                throw new TransactionRejectedException(nameof(tx.SkuBlockHash) + " field cannot be null", tx);
            }

            CheckSkuBlockHashAndTxIndex(tx.SkuBlockHash, tx.SkuTxIndex, TransactionAction.CreateSku, netIdentifier);
            CheckTransactionFee(tx, BlockchainConstants.TransferSupplyFee);
            CheckTokenBalance(tx.FromPubKey, netIdentifier, BlockchainConstants.TransferSupplyFee);
            CheckSupplyBalance(tx.FromPubKey, tx.SkuBlockHash, tx.SkuTxIndex, netIdentifier, tx.Amount);
        }

        private void ValidateDestroySupplyTransaction(StateTransaction tx, string netIdentifier)
        {
            if (String.IsNullOrWhiteSpace(tx.FromPubKey))
            {
                throw new TransactionRejectedException(nameof(tx.FromPubKey) + " field cannot be null", tx);
            }

            if (tx.ToPubKey != null)
            {
                throw new TransactionRejectedException(nameof(tx.ToPubKey) + " field must be null", tx);
            }

            CheckSkuBlockHashAndTxIndex(tx.SkuBlockHash, tx.SkuTxIndex, TransactionAction.CreateSku, netIdentifier);
            CheckTransactionFee(tx, BlockchainConstants.DestroySupplyFee);
            CheckTokenBalance(tx.FromPubKey, netIdentifier, BlockchainConstants.DestroySupplyFee);
            CheckSupplyBalance(tx.FromPubKey, tx.SkuBlockHash, tx.SkuTxIndex, netIdentifier, tx.Amount);
        }

        private void ValidateCreateSupplyTransaction(StateTransaction tx, string netIdentifier)
        {
            CheckFromAndToNotNull(tx);
            var originalTransaction = CheckSkuBlockHashAndTxIndex(tx.SkuBlockHash, tx.SkuTxIndex, TransactionAction.CreateSku, netIdentifier);
            
            if (tx.FromPubKey != originalTransaction.FromPubKey)
            {
                throw new TransactionRejectedException("Only the owner of the SKU is allowed to create supply", tx);
            }

            if (tx.Amount < 1)
            {
                throw new TransactionRejectedException("Minimum new supply must be 1", tx);
            }

            CheckTransactionFee(tx, BlockchainConstants.CreateSupplyFee);
            CheckTokenBalance(tx.FromPubKey, netIdentifier, BlockchainConstants.CreateSupplyFee);
        }

        private void ValidateCreateSkuTransaction(StateTransaction tx, string netIdentifier)
        {
            CheckFromAndToNotNull(tx);

            if (tx.FromPubKey != tx.ToPubKey)
            {
                throw new TransactionRejectedException(nameof(tx.FromPubKey) + " and " + nameof(tx.ToPubKey) + " fields must be equal", tx);
            }

            if (tx.SkuBlockHash != null)
            {
                throw new TransactionRejectedException(nameof(tx.SkuBlockHash) + " must be null", tx);
            }

            // Check SKU fields
            TryDeserializeSkuData(tx.Data, out SkuData skuData);

            if (String.IsNullOrWhiteSpace(skuData.SkuId))
            {
                throw new TransactionRejectedException(nameof(skuData.SkuId) + " field cannot be null or empty", tx);
            }

            if (String.IsNullOrWhiteSpace(skuData.EanCode))
            {
                throw new TransactionRejectedException(nameof(skuData.EanCode) + " field cannot be null or empty", tx);
            }

            CheckTransactionFee(tx, BlockchainConstants.CreateSkuFee);
            CheckTokenBalance(tx.FromPubKey, netIdentifier, BlockchainConstants.CreateSkuFee);
        }

        private void ValidateChangeSkuTransaction(StateTransaction tx, string netIdentifier)
        {
            throw new NotImplementedException();
        }

        private void ValidateCoinbaseTransaction(StateTransaction tx, string netIdentifier)
        {
            if (tx.FromPubKey != null)
            {
                throw new TransactionRejectedException(nameof(tx.FromPubKey) + " field must be null in a Coinbase transaction", tx);
            }

            if (tx.ToPubKey == null)
            {
                throw new TransactionRejectedException(nameof(tx.ToPubKey) + " field cannot be null", tx);
            }

            if (tx.Fee != 0)
            {
                throw new TransactionRejectedException("Fee must be zero on Coinbase transactions", tx);
            }

            if (tx.Amount > BlockchainConstants.CoinbaseReward)
            {
                throw new TransactionRejectedException("Coinbase reward is too high. Maximum: " + BlockchainConstants.CoinbaseReward, tx);
            }
        }

        // Helpers
        private void TryDeserializeSkuData(string data, out SkuData skuData)
        {
            try
            {
                skuData = JsonConvert.DeserializeObject<SkuData>(data);
            }
            catch (JsonReaderException)
            {
                throw new TransactionRejectedException("Invalid data contents: Unable to deserialize to SkuData object");
            }
        }

        private void CheckTransactionFee(StateTransaction tx, uint minimumFee)
        {
            if (tx.Fee < minimumFee)
            {
                throw new TransactionRejectedException("Fee is too low. Minimum fee is "+minimumFee+" tokens for this action", tx);
            }
        }

        private void CheckFromAndToNotNull(StateTransaction tx)
        {
            if (String.IsNullOrWhiteSpace(tx.FromPubKey))
            {
                throw new TransactionRejectedException(nameof(tx.FromPubKey) + " field cannot be null", tx);
            }

            if (String.IsNullOrWhiteSpace(tx.ToPubKey))
            {
                throw new TransactionRejectedException(nameof(tx.ToPubKey) + " field cannot be null", tx);
            }
        }

        private void CheckTokenBalance(string pubKey, string netId, ulong minimumBalance)
        {
            ulong currentBalance = _transactionRepo.GetTokenBalanceForPubKey(pubKey, netId);
            if (currentBalance < minimumBalance)
            {
                throw new TransactionRejectedException("Insufficient token balance");
            }
        }

        private StateTransaction CheckSkuBlockHashAndTxIndex(string skuBlockHash, int skuTxIndex, TransactionAction expectedTxAction, string netId)
        {
            try
            {
                var block = _blockchainRepository.GetBlockByHash(skuBlockHash, netId);
                var transaction = block.Transactions.ToList()[skuTxIndex];
                if (transaction.Action != expectedTxAction.ToString())
                {
                    throw new TransactionRejectedException("Invalid transaction action in SkuBlock. Expected action: "+ expectedTxAction.ToString());
                }

                return (StateTransaction)transaction;
            }
            catch(KeyNotFoundException e)
            {
                throw new TransactionRejectedException(e.Message);
            }
            catch (IndexOutOfRangeException)
            {
                throw new TransactionRejectedException("SKU transaction does not exist in SkuBlock");
            }
        }

        private void CheckSupplyBalance(string pubKey, string createdSkuBlockHash, int skuTxId, string netId, ulong minimumBalance)
        {
            ulong currentBalance = _skuRepo.GetSupplyBalanceForPubKey(pubKey, createdSkuBlockHash, skuTxId, netId);
            if (currentBalance < minimumBalance)
            {
                throw new TransactionRejectedException("Insufficient supply balance");
            }
        }

        private void CheckTxIsCorrectlyHashedAndSigned(StateTransaction tx)
        {
            if (!tx.IsFinalized())
            {
                throw new TransactionRejectedException("Transaction is not finalized", tx);
            }

            if (_txFinalizer.CalculateHash(tx) != tx.Hash)
            {
                throw new TransactionRejectedException(nameof(tx.Hash) + " is incorrect", tx);
            }
            
            if (_txFinalizer.CreateSignature(tx) != tx.Signature)
            {
                throw new TransactionRejectedException(nameof(tx.Signature) + " is incorrect", tx);
            }

            // todo finish signature check this with wallet implementation
            // if tx.action == coinbase, use the 'To' field for the signature. Otherwise, use the 'From' field.
        }
    }
}
