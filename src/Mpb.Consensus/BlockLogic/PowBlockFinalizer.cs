using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.BlockLogic
{
    /// <summary>
    /// Todo: This is not an actual finalizer (like cleaning up memory), but more of a block 'sealer'. Rename this class?
    /// Todo: Make this more efficient by only updating the nonce in the block header bytes.
    /// In order to achieve this, every field must have a predefined length so we can allocate them.
    /// </summary>
    public class PowBlockFinalizer : IBlockFinalizer
    {

        public virtual string CalculateHash(Block block)
        {
            using (var sha256 = SHA256.Create())
            {
                var blockHash = sha256.ComputeHash(GetBlockHeaderBytes(block));
                return BitConverter.ToString(blockHash).Replace("-", "");
            }
        }

        public virtual string CreateSignature(string hash, string privKey)
        {
            // Todo: sign the validhash with the privkey
            return "signature";
        }

        public virtual void FinalizeBlock(Block block, string validHash, string privKey)
        {
            var signature = CreateSignature(validHash, privKey);
            block.Header.Finalize(validHash, signature);
        }

        public virtual byte[] GetBlockHeaderBytes(Block block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block), "Block cannot be null");
            }

            // Accumulate all bytes (using BigEndian to support multiple platform architectures)
            byte[] magicNumberBytes = Encoding.BigEndianUnicode.GetBytes(block.Header.MagicNumber);
            byte[] versionBytes = Encoding.BigEndianUnicode.GetBytes(block.Header.Version.ToString());
            byte[] previousBlockHash = Encoding.BigEndianUnicode.GetBytes(block.Header.PreviousHash);
            byte[] merkleRootBytes = Encoding.BigEndianUnicode.GetBytes(block.Header.MerkleRoot);
            byte[] timestampBytes = Encoding.BigEndianUnicode.GetBytes(block.Header.Timestamp.ToString());
            byte[] nonceBytes = Encoding.BigEndianUnicode.GetBytes(block.Header.Nonce.ToString());
            byte[] transactionCountBytes = Encoding.BigEndianUnicode.GetBytes(block.Transactions.Count().ToString());
            var byteArrayLength = magicNumberBytes.Length + versionBytes.Length + previousBlockHash.Length
                + merkleRootBytes.Length + timestampBytes.Length + nonceBytes.Length + transactionCountBytes.Length;
            var array = new byte[byteArrayLength];

            // Copy the bytes to array
            Buffer.BlockCopy(magicNumberBytes, 0, array, 0, magicNumberBytes.Length);
            Buffer.BlockCopy(versionBytes, 0, array, magicNumberBytes.Length, versionBytes.Length);
            Buffer.BlockCopy(previousBlockHash, 0, array, magicNumberBytes.Length + versionBytes.Length, previousBlockHash.Length);
            Buffer.BlockCopy(merkleRootBytes, 0, array, magicNumberBytes.Length + previousBlockHash.Length + versionBytes.Length, merkleRootBytes.Length);
            Buffer.BlockCopy(timestampBytes, 0, array, magicNumberBytes.Length + previousBlockHash.Length + versionBytes.Length + merkleRootBytes.Length, timestampBytes.Length);
            Buffer.BlockCopy(nonceBytes, 0, array, magicNumberBytes.Length + previousBlockHash.Length + versionBytes.Length + merkleRootBytes.Length + timestampBytes.Length, nonceBytes.Length);
            Buffer.BlockCopy(transactionCountBytes, 0, array, array.Length - transactionCountBytes.Length, transactionCountBytes.Length);

            return array;
        }
    }
}
