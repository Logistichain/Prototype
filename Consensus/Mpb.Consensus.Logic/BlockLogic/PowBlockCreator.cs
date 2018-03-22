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
        private readonly PowBlockValidator _validator;

        public PowBlockCreator(ITimestamper timestamper, PowBlockValidator validator)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator)); ;
        }

        /// <summary>
        /// Mine a Proof-of-Work block by following the current consensus rules
        /// </summary>
        /// <param name="transactions">The transactions that will be included in the new block</param>
        /// <param name="difficulty">The difficulty to start with. Must be atleast 1</param>
        /// <returns>A valid block that meets the consensus conditions</returns>
        public virtual Block CreateValidBlock(IEnumerable<Transaction> transactions, BigDecimal difficulty)
        {
            return CreateValidBlock(BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget);
        }

        /// <summary>
        /// Mine a Proof-of-Work block with custom parameters
        /// </summary>
        /// <param name="netIdentifier">The net identifier for this block</param>
        /// <param name="protocolVersion">The current protocol version</param>
        /// <param name="transactions">The transactions that will be included in the new block</param>
        /// <param name="difficulty">The difficulty to start with. Must be atleast 1</param>
        /// <param name="maximumtarget">The maximum (easiest) target possible</param>
        /// <returns>A valid block that meets the consensus conditions, unless a different maximumTarget was given!</returns>
        public virtual Block CreateValidBlock(string netIdentifier, int protocolVersion, IEnumerable<Transaction> transactions, BigDecimal difficulty, BigDecimal maximumTarget)
        {
            if (difficulty < 1)
            {
                throw new DifficultyCalculationException("Difficulty cannot be zero.");
            }

            bool targetMet = false;
            var utcTimestamp = _timestamper.GetCurrentUtcTimestamp();
            Block b = new Block(netIdentifier, protocolVersion, "abc", utcTimestamp, transactions);
            var currentTarget = maximumTarget / difficulty;

            while (targetMet == false)
            {
                if (b.Nonce == ulong.MaxValue)
                {
                    throw new NonceLimitReachedException();
                }

                b.IncrementNonce();
                var sha256 = SHA256.Create();
                var blockHash = sha256.ComputeHash(GetBlockHeaderBytes(b));

                targetMet = _validator.BlockIsValid(b, currentTarget, blockHash);
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
