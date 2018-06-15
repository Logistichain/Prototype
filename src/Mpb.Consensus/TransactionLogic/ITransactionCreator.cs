using Mpb.Model;

namespace Mpb.Consensus.TransactionLogic
{
    /// <summary>
    /// Create transactions easily with this interface.
    /// </summary>
    public interface ITransactionCreator
    {
        /// <summary>
        /// Creates a CoinBase transaction. This transaction is required for every block.
        /// </summary>
        /// <param name="creatorPubKey">The public key of the block creator</param>
        /// <param name="creatorPrivKey">The private key of the block creator</param>
        /// <param name="optionalData">Free text field</param>
        /// <returns>A coinbase transaction</returns>
        AbstractTransaction CreateCoinBaseTransaction(string creatorPubKey, string creatorPrivKey, string optionalData);

        /// <summary>
        /// Creates a transaction that registers a new SKU.
        /// </summary>
        /// <param name="ownerPubKey">The public key of the address that owns the SKU and it's supply</param>
        /// <param name="ownerPrivKey">The private key of the SKU owner</param>
        /// <param name="amount">The initial supply to create</param>
        /// <param name="sku">The SKU data to register</param>
        /// <returns>A createsku transaction</returns>
        AbstractTransaction CreateSkuCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, SkuData sku);

        /// <summary>
        /// Creates a transaction that creates new supply for an existing SKU.
        /// </summary>
        /// <param name="ownerPubKey">The public key of the address that owns the SKU and it's supply</param>
        /// <param name="ownerPrivKey">The private key of the SKU owner</param>
        /// <param name="amount">The new supply to create</param>
        /// <param name="skuBlockHash">The block where the SKU was created</param>
        /// <param name="skuTxIndex">The transaction index of the given block where the SKU was created</param>
        /// <param name="optionalData">Free text field</param>
        /// <returns>A createsupply transaction</returns>
        AbstractTransaction CreateSupplyCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);
        
        /// <summary>
        /// Creates a transaction that destroys supply for an existing SKU.
        /// </summary>
        /// <param name="ownerPubKey">The public key of the address that owns the supply</param>
        /// <param name="ownerPrivKey">The private key of the supply owner</param>
        /// <param name="amount">The amount of supply to destroy</param>
        /// <param name="skuBlockHash">The block where the SKU was created</param>
        /// <param name="skuTxIndex">The transaction index of the given block where the SKU was created</param>
        /// <param name="optionalData">Free text field</param>
        /// <returns>A destroysupply transaction</returns>
        AbstractTransaction CreateSupplyDestroyTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);

        /// <summary>
        /// Creates a transaction that transfers supply to another address.
        /// </summary>
        /// <param name="fromPubKey">The public key of the address that owns the supply</param>
        /// <param name="fromPrivKey">The private key of the supply owner</param>
        /// <param name="toPubKey">The public key that receives the supply</param>
        /// <param name="amount">The amount of supply which will be transferred</param>
        /// <param name="skuBlockHash">The block where the SKU was created</param>
        /// <param name="skuTxIndex">The transaction index of the given block where the SKU was created</param>
        /// <param name="optionalData">Free text field</param>
        /// <returns>A createsupply transaction</returns>
        AbstractTransaction CreateSupplyTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);

        /// <summary>
        /// Creates a transaction that transfers tokens to another address.
        /// </summary>
        /// <param name="fromPubKey">The public key of the address that holds tokens</param>
        /// <param name="fromPrivKey">The private key of the token holder</param>
        /// <param name="toPubKey">The public key that receives the tokens</param>
        /// <param name="amount">The amount of tokens which will be transferred</param>
        /// <param name="optionalData">Free text field</param>
        /// <returns>A createsupply transaction</returns>
        AbstractTransaction CreateTokenTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string optionalData);
    }
}