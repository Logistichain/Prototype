using Mpb.Consensus.BlockLogic;
using Mpb.Model;
using Mpb.Shared.Constants;
using Newtonsoft.Json;
using System;

namespace Mpb.Consensus.TransactionLogic
{
    public class StateTransactionCreator : ITransactionCreator
    {
        private readonly ITransactionFinalizer _txFinalizer;

        public StateTransactionCreator(ITransactionFinalizer txFinalizer)
        {
            _txFinalizer = txFinalizer;
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
            _txFinalizer.FinalizeTransaction(tx, fromPrivKey);

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
            _txFinalizer.FinalizeTransaction(tx, ownerPrivKey);

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
            _txFinalizer.FinalizeTransaction(tx, ownerPrivKey);

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
            _txFinalizer.FinalizeTransaction(tx, fromPrivKey);

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
            _txFinalizer.FinalizeTransaction(tx, ownerPrivKey);

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
            _txFinalizer.FinalizeTransaction(tx, creatorPrivKey);

            return tx;
        }
    }
}
