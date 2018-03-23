using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.Logic.BlockLogic
{
    /// <summary>
    /// Todo: Make this more efficient by only updating the nonce in the block header bytes.
    /// In order to achieve this, every field must have a predefined length so we can allocate them.
    /// </summary>
    public class BlockHeaderHelper
    {
        public virtual byte[] GetBlockHeaderBytes(Block block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block), "Block cannot be null");
            }

            // Accumulate all bytes (using BigEndian to support multiple platform architectures)
            byte[] magicNumberBytes = Encoding.BigEndianUnicode.GetBytes(block.MagicNumber);
            byte[] versionBytes = Encoding.BigEndianUnicode.GetBytes(block.Version.ToString());
            byte[] merkleRootBytes = Encoding.BigEndianUnicode.GetBytes(block.MerkleRoot);
            byte[] timestampBytes = Encoding.BigEndianUnicode.GetBytes(block.Timestamp.ToString());
            byte[] nonceBytes = Encoding.BigEndianUnicode.GetBytes(block.Nonce.ToString());
            byte[] transactionCountBytes = Encoding.BigEndianUnicode.GetBytes(block.Transactions.Count().ToString());
            var byteArrayLength = magicNumberBytes.Length + versionBytes.Length + merkleRootBytes.Length
                + timestampBytes.Length + nonceBytes.Length + transactionCountBytes.Length;
            var array = new byte[byteArrayLength];

            // Copy the bytes to array
            Buffer.BlockCopy(magicNumberBytes, 0, array, 0, magicNumberBytes.Length);
            Buffer.BlockCopy(versionBytes, 0, array, magicNumberBytes.Length, versionBytes.Length);
            Buffer.BlockCopy(merkleRootBytes, 0, array, magicNumberBytes.Length + versionBytes.Length, merkleRootBytes.Length);
            Buffer.BlockCopy(timestampBytes, 0, array, magicNumberBytes.Length + versionBytes.Length + merkleRootBytes.Length, timestampBytes.Length);
            Buffer.BlockCopy(nonceBytes, 0, array, magicNumberBytes.Length + versionBytes.Length + merkleRootBytes.Length + timestampBytes.Length, nonceBytes.Length);
            Buffer.BlockCopy(transactionCountBytes, 0, array, array.Length - transactionCountBytes.Length, transactionCountBytes.Length);

            return array;
        }
    }
}
