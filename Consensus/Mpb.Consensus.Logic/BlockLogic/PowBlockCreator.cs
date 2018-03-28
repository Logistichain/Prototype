using System;
using System.Collections.Generic;
using Mpb.Consensus.Contract;
using Mpb.Consensus.Model;
using System.Security.Cryptography;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Logic.Constants;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class PowBlockCreator
    {
        private readonly ITimestamper _timestamper;
        private readonly PowBlockValidator _validator;
        private readonly BlockHeaderHelper _blockHeaderHelper;

        public PowBlockCreator(ITimestamper timestamper, PowBlockValidator validator, BlockHeaderHelper blockHeaderHelper)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _blockHeaderHelper = blockHeaderHelper ?? throw new ArgumentNullException(nameof(blockHeaderHelper));
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
                var blockHash = sha256.ComputeHash(_blockHeaderHelper.GetBlockHeaderBytes(b));
                b.SetHash(blockHash);

                try
                {
                    _validator.ValidateBlock(b, currentTarget, false);
                    targetMet = true;
                }
                catch (BlockRejectedException ex)
                {
                    // Todo: Log, but continue
                }
            }

            return b;
        }
    }
}
