using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.Constants;
using Mpb.Consensus.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.Logic.TransactionLogic
{
    public class StateTransactionCreator : ITransactionCreator
    {
        private readonly TransactionByteConverter _byteConverter;

        public StateTransactionCreator(TransactionByteConverter byteConverter)
        {
            _byteConverter = byteConverter;
        }

        /// <summary>
        /// Create a TransferToken transaction, following the current consensus rules
        /// </summary>
        /// <param name="fromPubKey">The sender's public key</param>
        /// <param name="fromPrivKey">The sender's private key to sign the transaction</param>
        /// <param name="toPubKey">The receiver's public key</param>
        /// <param name="amount">The amount of tokens to transfer</param>
        /// <param name="optionalData">Space for data</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateTokenTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string optionalData)
        {
            var tx = new StateTransaction(
                fromPubKey,
                toPubKey,
                null,
                0,
                amount,
                BlockchainConstants.TransactionVersion,
                TransactionAction.TransferToken.ToString(),
                optionalData,
                BlockchainConstants.TransferTokenFee
                );
            FinalizeTransaction(tx, fromPubKey, fromPrivKey);

            return tx;
        }

        /// <summary>
        /// Register a new SKU with this transaction, following the current consensus rules
        /// </summary>
        /// <param name="ownerPubKey">The owner of the SKU and it's supply</param>
        /// <param name="ownerPrivKey">The owner's private key to sign the transaction</param>
        /// <param name="amount">The initial supply (amount of SKU's)</param>
        /// <param name="sku">This SKU will be generated</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateSkuCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, SkuData sku)
        {
            var skuJson = JsonConvert.SerializeObject(sku);

            var tx = new StateTransaction(
                ownerPubKey,
                ownerPubKey,
                null,
                0,
                amount,
                BlockchainConstants.TransactionVersion,
                TransactionAction.CreateSku.ToString(),
                skuJson,
                BlockchainConstants.CreateSkuFee
                );
            FinalizeTransaction(tx, ownerPubKey, ownerPrivKey);

            return tx;
        }

        /// <summary>
        /// Create new supply for a given SKU with this transaction, following the current consensus rules
        /// </summary>
        /// <param name="ownerPubKey">The owner of the SKU and it's supply</param>
        /// <param name="ownerPrivKey">The owner's private key to sign the transaction</param>
        /// <param name="amount">The initial supply (amount of SKU's)</param>
        /// <param name="skuBlockHash">The block where the (latest) SKU create/change transaction resides</param>
        /// <param name="skuTxIndex">The transaction index from the SkuBlock, containing the SKU data</param>
        /// <param name="optionalData">Space for data</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateSupplyCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData)
        {
            var tx = new StateTransaction(
                ownerPubKey,
                ownerPubKey,
                skuBlockHash,
                skuTxIndex,
                amount,
                BlockchainConstants.TransactionVersion,
                TransactionAction.CreateSupply.ToString(),
                optionalData,
                BlockchainConstants.CreateSupplyFee
                );
            FinalizeTransaction(tx, ownerPubKey, ownerPrivKey);

            return tx;
        }

        /// <summary>
        /// Create new supply for a given SKU with this transaction, following the current consensus rules.
        /// The new supply will be available to the owner after this transaction has been confirmed in the blockchain.
        /// </summary>
        /// <param name="fromPubKey">The owner of the supply</param>
        /// <param name="fromPrivKey">The owner's private key to sign the transaction</param>
        /// <param name="toPubKey">The public key to send the supply to</param>
        /// <param name="amount">The initial supply (amount of SKU's)</param
        /// <param name="skuBlockHash">The block where the (latest) SKU transaction resides</param>
        /// <param name="skuTxIndex">The transaction index from the SkuBlock, containing the SKU data</param>
        /// <param name="optionalData">Space for data</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateSupplyTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData)
        {
            var tx = new StateTransaction(
                fromPubKey,
                toPubKey,
                skuBlockHash,
                skuTxIndex,
                amount,
                BlockchainConstants.TransactionVersion,
                TransactionAction.TransferSupply.ToString(),
                optionalData,
                BlockchainConstants.TransferSupplyFee
                );
            FinalizeTransaction(tx, fromPubKey, fromPrivKey);

            return tx;
        }

        /// <summary>
        /// Destroy supply, following the current consensus rules
        /// </summary>
        /// <param name="ownerPubKey">The current holder of the supply</param>
        /// <param name="ownerPrivKey">The owner's private key to sign the transaction</param>
        /// <param name="amount">The amount of supply to destroy</param>
        /// <param name="skuBlockHash">The block where the (latest) SKU transaction resides</param>
        /// <param name="skuTxIndex">The transaction index from the SkuBlock, containing the SKU data</param>
        /// <param name="optionalData">Space for data</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateSupplyDestroyTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData)
        {
            var tx = new StateTransaction(
                ownerPubKey,
                null,
                null,
                0,
                amount,
                BlockchainConstants.TransactionVersion,
                TransactionAction.DestroySupply.ToString(),
                optionalData,
                BlockchainConstants.DestroySupplyFee
                );
            FinalizeTransaction(tx, ownerPubKey, ownerPrivKey);

            return tx;
        }

        /// <summary>
        /// Create a Coinbase transaction, following the current consensus rules
        /// </summary>
        /// <param name="creatorPubKey">Creator's public key of the coinbase reward</param>
        /// <param name="creatorPrivKey">Creator's private key to sign the transaction</param>
        /// <param name="optionalData">Space for data</param>
        /// <returns>A transaction object</returns>
        public AbstractTransaction CreateCoinBaseTransaction(string creatorPubKey, string creatorPrivKey, string optionalData)
        {
            var tx = new StateTransaction(
                null,
                creatorPubKey,
                null,
                0,
                BlockchainConstants.CoinbaseReward,
                1,
                TransactionAction.ClaimCoinbase.ToString(),
                optionalData,
                0
                );
            FinalizeTransaction(tx, creatorPubKey, creatorPrivKey);

            return tx;
        }

        //! Signature is always "" until an appropriate wallet module can be utilized.
        /// <summary>
        /// Create a hash for the entire transaction object and sign that hash
        /// with the private key from the sender.
        /// </summary>
        /// <param name="tx">The transaction to hash and sign</param>
        /// <param name="fromPubKey">The creator of the transaction</param>
        /// <param name="fromPrivKey">The creator's private key to sign the transaction hash</param>
        private void FinalizeTransaction(AbstractTransaction tx, string fromPubKey, string fromPrivKey)
        {
            if (tx.IsFinalized()) { return; }
            
            var txByteArray = _byteConverter.GetTransactionBytes(tx);
            var hashString = "";
            var signature = ""; // Todo dependency inject wallet mechanism to sign the transaction!
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(txByteArray);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }

            tx.FinalizeTransaction(hashString, signature);
        }
    }
}
