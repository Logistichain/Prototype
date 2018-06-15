using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Mpb.Model;
using System.Security.Cryptography;
using Mpb.Consensus.Exceptions;
using System.Linq;
using Mpb.Consensus.TransactionLogic;
using Mpb.Consensus.MiscLogic;
using Mpb.Shared;
using Mpb.Shared.Constants;
using Mpb.DAL;
using Mpb.Consensus.Cryptography;

namespace Mpb.Consensus.BlockLogic
{
    public class PowBlockValidator : IBlockValidator
    {
        private readonly IBlockFinalizer _blockFinalizer;
        private readonly ITransactionValidator _transactionValidator;
        private readonly ITimestamper _timestamper;
        private readonly ISigner _signer;

        public PowBlockValidator(IBlockFinalizer blockFinalizer, ITransactionValidator transactionValidator, ITimestamper timestamper, ISigner signer)
        {
            _blockFinalizer = blockFinalizer ?? throw new ArgumentNullException(nameof(blockFinalizer));
            _transactionValidator = transactionValidator ?? throw new ArgumentNullException(nameof(transactionValidator));
            _timestamper = timestamper ?? throw new ArgumentNullException(nameof(timestamper));
            _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        }

        //! Chain of Responsibility pattern could be possible here. Only check for PoW things, then call the next one for more generic checks
        public virtual void ValidateBlock(Block block, BigDecimal currentTarget, Blockchain blockchain, bool checkTimestamp, bool writeToBlockchain)
        {
            BigInteger.TryParse(block.Header.Hash, NumberStyles.HexNumber, new CultureInfo("en-US"), out var hashValue);
            ValidateBlockHeader(block, hashValue, currentTarget, checkTimestamp, blockchain.NetIdentifier);

            // Transaction list may not be empty
            if (block.Transactions.Count() == 0)
            {
                throw new BlockRejectedException("Transaction list cannot be empty", block);
            }

            // Check merkleroot
            var calculatedMerkleRoot = _transactionValidator.CalculateMerkleRoot(block.Transactions.ToList());
            if (block.Header.MerkleRoot != calculatedMerkleRoot)
            {
                throw new BlockRejectedException("Incorrect merkleroot", block);
            }

            // First transaction must be coinbase
            var firstTransaction = (StateTransaction)block.Transactions.First();
            if (firstTransaction.Action != TransactionAction.ClaimCoinbase.ToString())
            {
                throw new BlockRejectedException("First transaction is not coinbase", block);
            }

            // Only one coinbase transaction may exist
            if (block.Transactions.Where(tx => tx.Action == TransactionAction.ClaimCoinbase.ToString()).Count() > 1)
            {
                throw new BlockRejectedException("Multiple coinbase transactions found", block);
            }

            // Check the signature to make sure the block wasn't altered
            if (!_signer.SignatureIsValid(block.Header.Signature, block.Header.Hash, firstTransaction.ToPubKey))
            {
                throw new BlockRejectedException("Block's signature is invalid", block);
            }

            // Check if the previous hash exists in our blockchain
            // Todo if the previous hash is unknown, let Networking retrieve the entire blockchain (don't do that here, but the caller must catch the exception and do it there)
            lock (blockchain)
            {
                if (blockchain.Blocks.Where(b => b.Header.Hash == block.Header.PreviousHash).Count() == 0 && blockchain.CurrentHeight > -1)
                {
                    throw new BlockRejectedException("Previous blockhash does not exist in our chain", block);
                }
            }

            // Check all other transactions
            foreach (var tx in block.Transactions)
            {
                _transactionValidator.ValidateTransaction(tx);
            }

            // Check if the previous hash isn't used by another block
            // If it is used by another block, and that block is the latest,
            // then check which block has the best difficulty.
            lock (blockchain)
            {
                if (blockchain.CurrentHeight > -1)
                {
                    var existingBlocks = blockchain.Blocks.Where(b => b.Header.PreviousHash == block.Header.PreviousHash);
                    for (int i = 0; i < existingBlocks.Count(); i++)
                    {
                        var existingBlock = existingBlocks.ElementAt(i);
                        int heightInChain = blockchain.GetHeightForBlock(existingBlock.Header.Hash);

                        if (blockchain.CurrentHeight == heightInChain) 
                        {
                            // This is the latest block so we might replace it. Determine the difficulty.
                            BigDecimal existingBlockHashValue = BigInteger.Parse(existingBlock.Header.Hash, NumberStyles.HexNumber);
                            if (hashValue < existingBlockHashValue)
                            {
                                if (writeToBlockchain)
                                {
                                    blockchain.Blocks.Remove(existingBlock);
                                }
                            }
                            else
                            {
                                throw new BlockRejectedException("Another block with higher difficulty points to the same PreviousHash", block);
                            }
                        }
                        else
                        {
                            throw new BlockRejectedException("Chain splitting is not supported", block);
                        }
                    }
                }
                
                // Finished
                if (writeToBlockchain)
                {
                    blockchain.Blocks.Add(block);
                }
            }
        }

        public virtual void ValidateBlockHeader(Block block, BigDecimal hashValue, BigDecimal currentTarget, bool checkTimestamp, string netId)
        {
            // The block must be in the same network as our node
            if (block.Header.MagicNumber != netId)
            {
                throw new BlockRejectedException("Block comes from a different network", block);
            }

            if (!block.Header.IsFinalized())
            {
                throw new BlockRejectedException("Block is not hashed or signed or hashed properly", block);
            }
            else if (block.Header.Hash != _blockFinalizer.CalculateHash(block))
            {
                throw new BlockRejectedException("The hash property of the block is not equal to the calculated hash", block);
            }

            // Hash value must be lower than the target and the first byte must be zero
            // because the first byte indidates if the hashValue is a positive or negative number,
            // negative numbers are not allowed.
            if (!block.Header.Hash.StartsWith("0"))
            {
                throw new BlockRejectedException("Hash has no leading zero", block);
            }

            // The hash value must be lower than the given target
            if (hashValue >= currentTarget)
            {
                throw new BlockRejectedException("Hash value is equal or higher than the current target", block);
            }

            if (!checkTimestamp) return;

            // Timestamp must not be lower than UTC - 2 min and not higher than UTC + 2 min
            if (_timestamper.GetCurrentUtcTimestamp() - block.Header.Timestamp > BlockchainConstants.MaximumTimestampOffset || _timestamper.GetCurrentUtcTimestamp() - block.Header.Timestamp < (BlockchainConstants.MaximumTimestampOffset * -1))
            {
                throw new BlockRejectedException("Block timestamp differs too much");
            }
        }
    }
}
