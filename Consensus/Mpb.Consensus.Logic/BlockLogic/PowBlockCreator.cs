using System;
using System.Collections.Generic;
using Mpb.Consensus.Model;
using System.Security.Cryptography;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Logic.Constants;
using Mpb.Consensus.Logic.MiscLogic;
using System.Threading;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class PowBlockCreator : IPowBlockCreator
    {
        private readonly ITimestamper _timestamper;
        private readonly IBlockValidator _validator;
        private readonly IBlockHeaderHelper _blockHeaderHelper;

        public PowBlockCreator(ITimestamper timestamper, IBlockValidator validator, IBlockHeaderHelper blockHeaderHelper)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _blockHeaderHelper = blockHeaderHelper ?? throw new ArgumentNullException(nameof(blockHeaderHelper));
        }

        public virtual Block CreateValidBlock(IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty)
        {
            return CreateValidBlock(BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, CancellationToken.None);
        }
        
        public virtual Block CreateValidBlock(IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, CancellationToken ct)
        {
            return CreateValidBlock(BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, ct);
        }

        public virtual Block CreateValidBlock(string netIdentifier, uint protocolVersion, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, BigDecimal maximumTarget, CancellationToken ct)
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
                ct.ThrowIfCancellationRequested();

                if (b.Nonce == ulong.MaxValue)
                {
                    throw new NonceLimitReachedException();
                }

                b.IncrementNonce();
                using (var sha256 = SHA256.Create())
                {
                    var blockHash = sha256.ComputeHash(_blockHeaderHelper.GetBlockHeaderBytes(b));
                    b.SetHash(BitConverter.ToString(blockHash).Replace("-", ""));
                }

                try
                {
                    _validator.ValidateBlock(b, currentTarget, false);
                    targetMet = true;
                }
                catch (BlockRejectedException ex)
                {
                    // Todo Log
                    if (ex.Message != "Hash has no leading zero" && ex.Message != "Hash value is equal or higher than the current target")
                    {
                        throw;
                    }
                }
            }

            return b;
        }
    }
}
