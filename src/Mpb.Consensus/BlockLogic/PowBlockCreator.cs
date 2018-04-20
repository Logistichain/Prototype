using System;
using System.Collections.Generic;
using Mpb.Model;
using Mpb.Consensus.Exceptions;
using Mpb.Consensus.MiscLogic;
using System.Threading;
using Mpb.Shared;
using Mpb.Shared.Constants;
using Mpb.Consensus.TransactionLogic;
using System.Linq;

namespace Mpb.Consensus.BlockLogic
{
    public class PowBlockCreator : IPowBlockCreator
    {
        private readonly ITimestamper _timestamper;
        private readonly IBlockValidator _validator;
        private readonly IBlockFinalizer _blockFinalizer;
        private readonly ITransactionValidator _transactionValidator;

        public PowBlockCreator(ITimestamper timestamper, IBlockValidator validator, IBlockFinalizer blockHeaderHelper, ITransactionValidator transactionValidator)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _blockFinalizer = blockHeaderHelper ?? throw new ArgumentNullException(nameof(blockHeaderHelper));
            _transactionValidator = transactionValidator ?? throw new ArgumentNullException(nameof(transactionValidator));
        }

        public virtual Block CreateValidBlock(string privateKey, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty)
        {
            return CreateValidBlock(privateKey, BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, CancellationToken.None);
        }
        
        public virtual Block CreateValidBlock(string privateKey, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, CancellationToken ct)
        {
            return CreateValidBlock(privateKey, BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, ct);
        }

        public virtual Block CreateValidBlock(string privateKey, string netIdentifier, uint protocolVersion, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, BigDecimal maximumTarget, CancellationToken ct)
        {
            if (difficulty < 1)
            {
                throw new DifficultyCalculationException("Difficulty cannot be zero.");
            }

            bool targetMet = false;
            var utcTimestamp = _timestamper.GetCurrentUtcTimestamp();
            var merkleroot = _transactionValidator.CalculateMerkleRoot(transactions.ToList());
            Block b = new Block(netIdentifier, protocolVersion, merkleroot, utcTimestamp, transactions);
            var currentTarget = maximumTarget / difficulty;

            // Keep on mining
            while (targetMet == false)
            {
                ct.ThrowIfCancellationRequested();

                if (b.Nonce == ulong.MaxValue)
                {
                    throw new NonceLimitReachedException();
                }

                b.IncrementNonce();
                var hash = _blockFinalizer.CalculateHash(b);
                _blockFinalizer.FinalizeBlock(b, hash, privateKey);

                try
                {
                    _validator.ValidateBlock(b, currentTarget);
                    targetMet = true;
                }
                catch (BlockRejectedException ex)
                {
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
