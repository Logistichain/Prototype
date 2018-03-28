using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Mpb.Consensus.Model;
using System.Security.Cryptography;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Contract;
using System.Linq;
using Mpb.Consensus.Logic.TransactionLogic;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class PowBlockValidator
    {
        private readonly BlockHeaderHelper _blockHeaderHelper;
        private readonly TransactionValidator _transactionValidator;
        private readonly ITimestamper _timestamper;

        public PowBlockValidator(BlockHeaderHelper blockHeaderHelper, TransactionValidator transactionValidator, ITimestamper timestamper)
        {
            _blockHeaderHelper = blockHeaderHelper ?? throw new ArgumentNullException(nameof(blockHeaderHelper));
            _transactionValidator = transactionValidator ?? throw new ArgumentNullException(nameof(transactionValidator));
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
        }

        // Todo Implement a proper merkle root algorithm
        /// <summary>
        /// This method creates the hash for the entire given transaction list.
        /// If something changes, like the order or even a single bit in a transaction,
        /// the output will be completely different. This method is used to 'seal' all
        /// transactions before signing.
        /// </summary>
        /// <param name="orderedTransactions"></param>
        /// <returns></returns>
        public virtual byte[] CalculateMerkleRoot(ICollection<Transaction> orderedTransactions)
        {

        }

        //! Decorator/composite pattern could be possible here. Only check for PoW things, then call the parent for more generic checks
        /// <summary>
        /// Validates a block. Throws BlockRejectedException if the validation fails.
        /// </summary>
        /// <param name="b">The block</param>
        /// <param name="currentTarget">The current target to validate the block's hash</param>
        /// <param name="setBlockHash">Whether this method needs to calculate and add the hash to the block</param>
        public virtual void ValidateBlock(Block b, BigDecimal currentTarget, bool setBlockHash)
        {
            if (setBlockHash)
            {
                b.SetHash(GetBlockHash(b));
            }
            else if (b.Hash == null)
            {
                throw new ArgumentNullException(nameof(b.Hash));
            }
            else if (b.Hash != GetBlockHash(b))
            {
                throw new BlockRejectedException("The hash property of the block is not equal to the calculated hash", b);
            }

            var hashString = BitConverter.ToString(b.Hash).Replace("-", "");
            BigDecimal hashValue = BigInteger.Parse(hashString, NumberStyles.HexNumber);

            // Hash value must be lower than the target and the first byte must be zero
            // because the first byte indidates if the hashValue is a positive or negative number,
            // negative numbers are not allowed.
            if (!hashString.StartsWith("0"))
            {
                throw new BlockRejectedException("Hash has no leading zero", b);
            }

            // The hash value must be lower than the given target
            if (hashValue >= currentTarget)
            {
                throw new BlockRejectedException("Hash value is equal or higher than the current target", b);
            }

            // Timestamp must not be lower than UTC - 2 min and not higher than UTC + 2 min
            // Todo refactor 120 seconds to blockchainconstant
            if (_timestamper.GetCurrentUtcTimestamp() - b.Timestamp > 120 || _timestamper.GetCurrentUtcTimestamp() - b.Timestamp < -120)
            {
                throw new BlockRejectedException("Timestamp is not within the acceptable range of -120 seconds and +120 seconds", b);
            }

            // Transaction list may not be empty
            if (b.Transactions.Count() == 0)
            {
                throw new BlockRejectedException("Transaction list cannot be empty", b);
            }

            // Check merkleroot

            // First transaction must be coinbase

            // Check all other transactions

            // Check if the previous hash exists in our blockchain
            // Todo if the previous hash is unknown, let the network module ask the entire 

            // Check if the previous hash isn't used by another block
            // If it is used by another block, and that block is the latest 
        }

        private byte[] GetBlockHash(Block b)
        {
            var sha256 = SHA256.Create();
            var blockHash = sha256.ComputeHash(_blockHeaderHelper.GetBlockHeaderBytes(b));
            return blockHash;
        }
    }
}
