using Mpb.Model;

namespace Mpb.Consensus.BlockLogic
{
    /// <summary>
    /// The BlockFinalizer 'seals' a block by setting the hash and signing the block with a private key.
    /// </summary>
    public interface IBlockFinalizer
    {
        /// <summary>
        /// Transforms block fields to one big-endian byte array.
        /// Fields: MagicNumber, Version, PreviousBlockHash, MerkleRoot, Timestamp, Nonce, TransactionCount
        /// </summary>
        /// <param name="block">The block to extract the field values from</param>
        /// <returns>Byte array of the the mentioned fields (concatenated)</returns>
        byte[] GetBlockHeaderBytes(Block block);

        // Todo Move to - or use - wallet/signing class
        /// <summary>
        /// Seals the block by setting the hash and signing the hash with the given private key.
        /// This method uses the CreateSignature method to sign the block.
        /// </summary>
        /// <param name="block">The block to finalize</param>
        /// <param name="validHash">The valid hash for this block (use the CalculateHash method)</param>
        /// <param name="privKey">The private key, which corresponds to the public key from the coinbase transaction</param>
        void FinalizeBlock(Block block, string validHash, string privKey);

        /// <summary>
        /// Signs (encrypts) the given hash value with the private key.
        /// The signature can be verified by using the corresponding public key.
        /// </summary>
        /// <param name="hash">The value to sign</param>
        /// <param name="privKey">The private key which will be used to sign the hash value</param>
        /// <returns>The signature</returns>
        string CreateSignature(string hash, string privKey);

        /// <summary>
        /// Calculates the hash by getting the header bytes and running a SHA-256 algoritm over it.
        /// </summary>
        /// <param name="block">The block to hash</param>
        /// <returns>The hash (uppercase, without dashes)</returns>
        string CalculateHash(Block block);
    }
}