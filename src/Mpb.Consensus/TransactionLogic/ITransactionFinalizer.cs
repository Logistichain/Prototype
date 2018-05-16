using Mpb.Model;

namespace Mpb.Consensus.BlockLogic
{
    /// <summary>
    /// The TransactionFinalizer 'seals' a block by setting the hash and signing the block with a private key.
    /// </summary>
    public interface ITransactionFinalizer
    {
        /// <summary>
        /// Adapter to convert a transaction to a byte array.
        /// </summary>
        string CalculateHash(AbstractTransaction transaction);

        // Todo Move to wallet/signing class
        /// <summary>
        /// Signs (encrypts) the given hash value with the private key.
        /// The signature can be verified by using the corresponding public key.
        /// </summary>
        /// <param name="hash">The transaction hash to sign</param>
        /// <param name="privKey">The private key which will be used to sign the hash value</param>
        /// <returns>The signature</returns>
        string CreateSignature(string hash, string privKey);
        
        /// <summary>
        /// Create a hash for the entire transaction object and sign that hash
        /// with the private key from the sender. The given transaction object will be updated.
        /// </summary>
        /// <param name="tx">The transaction to hash and sign</param>
        /// <param name="fromPrivKey">The creator's private key to sign the transaction hash</param>
        void FinalizeTransaction(AbstractTransaction tx, string fromPrivKey);

        /// <summary>
        /// Transforms transaction fields to one big-endian byte array.
        /// </summary>
        /// <param name="transaction">The transaction to extract the field values from</param>
        /// <returns>Byte array of the the fields (concatenated)</returns>
        byte[] GetTransactionBytes(AbstractTransaction transaction);
    }
}