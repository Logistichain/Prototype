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
using Mpb.Shared.Events;

namespace Mpb.Consensus.BlockLogic
{
    public class PowBlockCreator : IPowBlockCreator
    {
        private readonly ITimestamper _timestamper;
        private readonly IBlockValidator _validator;
        private readonly IBlockFinalizer _blockFinalizer;
        private readonly ITransactionValidator _transactionValidator;
        private bool hotRestart = false;

        public PowBlockCreator(ITimestamper timestamper, IBlockValidator validator, IBlockFinalizer blockFinalizer, ITransactionValidator transactionValidator)
        {
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _blockFinalizer = blockFinalizer ?? throw new ArgumentNullException(nameof(blockFinalizer));
            _transactionValidator = transactionValidator ?? throw new ArgumentNullException(nameof(transactionValidator));
            EventPublisher.GetInstance().OnValidatedBlockCreated += OnValidatedBlockCreated;
        }

        private void OnValidatedBlockCreated(object sender, BlockCreatedEventArgs eventHandler)
        {
            hotRestart = true;
        }

        public virtual Block CreateValidBlockAndAddToChain(string privateKey, Blockchain blockchain, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty)
        {
            return CreateValidBlockAndAddToChain(privateKey, blockchain, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, CancellationToken.None);
        }
        
        public virtual Block CreateValidBlockAndAddToChain(string privateKey, Blockchain blockchain, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, CancellationToken ct)
        {
            return CreateValidBlockAndAddToChain(privateKey, blockchain, BlockchainConstants.ProtocolVersion, transactions, difficulty, BlockchainConstants.MaximumTarget, ct);
        }

        public virtual Block CreateValidBlockAndAddToChain(string privateKey, Blockchain blockchain, uint protocolVersion, IEnumerable<AbstractTransaction> transactions, BigDecimal difficulty, BigDecimal maximumTarget, CancellationToken ct)
        {
            if (difficulty < 1)
            {
                throw new DifficultyCalculationException("Difficulty cannot be zero.");
            }

            bool targetMet = false;
            var currentTarget = maximumTarget / difficulty;
            Block b = PrepareBlock(blockchain, protocolVersion, transactions, currentTarget);

            // Keep on mining
            while (targetMet == false)
            {
                ct.ThrowIfCancellationRequested();

                if (hotRestart)
                {
                    b = PrepareBlock(blockchain, protocolVersion, transactions, currentTarget);
                    hotRestart = false;
                }

                if (b.Header.Nonce == ulong.MaxValue)
                {
                    throw new NonceLimitReachedException();
                }

                b.Header.IncrementNonce();
                var hash = _blockFinalizer.CalculateHash(b);
                _blockFinalizer.FinalizeBlock(b, hash, privateKey);

                try
                {
                    _validator.ValidateBlock(b, currentTarget, blockchain, true, true);
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

        private Block PrepareBlock(Blockchain blockchain, uint protocolVersion, IEnumerable<AbstractTransaction> transactions, BigDecimal currentTarget)
        {
            var utcTimestamp = _timestamper.GetCurrentUtcTimestamp();
            var merkleroot = _transactionValidator.CalculateMerkleRoot(transactions.ToList());
            string previousBlockHash = blockchain.Blocks.Count() > 0 ? blockchain.Blocks.Last().Header.Hash : null;
            return new Block(new BlockHeader(blockchain.NetIdentifier, protocolVersion, merkleroot, utcTimestamp, previousBlockHash), transactions);
        }
    }
}
