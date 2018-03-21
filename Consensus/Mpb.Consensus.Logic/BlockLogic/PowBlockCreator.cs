using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mpb.Consensus.Contract;
using Mpb.Consensus.Model;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Globalization;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Logic.Constants;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class PowBlockCreator
    {
        private readonly ITimestamper _timestamper;

        public PowBlockCreator(ITimestamper timestamper)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
        }

        /// <summary>
        /// Mine a Proof-of-Work block
        /// </summary>
        /// <param name="difficulty">The target to start with</param>
        /// <returns>A valid block that meets the consensus conditions</returns>
        public Block CreateValidBlock(BigDecimal difficulty)
        {
            bool targetMet = false;
            var utcTimestamp = _timestamper.GetCurrentUtcTimestamp();
            Block b = new Block("testnet", 1, "abc", utcTimestamp, new List<Transaction>());
            var currentTarget = BlockchainConstants.MaximumTarget / difficulty;

            while (targetMet == false)
            {
                if (b.Nonce == ulong.MaxValue)
                {
                    throw new NonceLimitReachedException();
                }

                b.IncrementNonce();
                var sha256 = SHA256.Create();
                var blockHash = sha256.ComputeHash(GetBlockHeaderBytes(b));

                var hashString = BitConverter.ToString(blockHash).Replace("-", "");
                BigDecimal hashValue = BigInteger.Parse(hashString, NumberStyles.HexNumber);

                // Hash value must be lower than the target and the first byte must be zero
                // because the first byte indidates if the hashValue is a positive or negative number,
                // negative numbers are not allowed.
                if (hashValue < currentTarget && hashString.StartsWith("0"))
                {
                    targetMet = true;
                }
            }

            return b;
        }

        public byte[] GetBlockHeaderBytes(Block b)
        {
            // Accumulate all bytes (using BigEndian to support multiple platform architectures)
            byte[] magicNumberBytes = Encoding.BigEndianUnicode.GetBytes(b.MagicNumber);
            byte[] versionBytes = Encoding.BigEndianUnicode.GetBytes(b.Version.ToString());
            byte[] merkleRootBytes = Encoding.BigEndianUnicode.GetBytes(b.MerkleRoot);
            byte[] timestampBytes = Encoding.BigEndianUnicode.GetBytes(b.Timestamp.ToString());
            byte[] nonceBytes = Encoding.BigEndianUnicode.GetBytes(b.Nonce.ToString());
            byte[] transactionCountBytes = Encoding.BigEndianUnicode.GetBytes(b.Transactions.Count().ToString());
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
