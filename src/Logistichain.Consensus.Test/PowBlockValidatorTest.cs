using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using Logistichain.Consensus.BlockLogic;
using Logistichain.Consensus.MiscLogic;
using Logistichain.Consensus.TransactionLogic;
using Logistichain.Shared;
using Logistichain.Model;
using Logistichain.Consensus.Exceptions;
using Logistichain.Shared.Constants;
using System.Linq;
using Logistichain.Consensus.Cryptography;

namespace Logistichain.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// Todo: Cover the scenario where the timestamp isn't checked.
    /// </summary>
    [TestClass]
    public class PowBlockValidatorTest
    {
        Mock<IBlockFinalizer> _blockFinalizerMock; // Using implementation because some tests require calling the base.
        Mock<ITimestamper> _timestamperMock;
        Mock<ITransactionValidator> _transactionValidatorMock;
        Mock<IDifficultyCalculator> _difficultyCalculatorMock;
        Mock<ISigner> _signerMock;
        string _netId;

        [TestInitialize]
        public void Initialize()
        {
            _blockFinalizerMock = new Mock<IBlockFinalizer>(MockBehavior.Strict);
            _timestamperMock = new Mock<ITimestamper>(MockBehavior.Strict);
            _transactionValidatorMock = new Mock<ITransactionValidator>(MockBehavior.Strict);
            _difficultyCalculatorMock = new Mock<IDifficultyCalculator>(MockBehavior.Strict);
            _signerMock = new Mock<ISigner>(MockBehavior.Strict);
            _netId = "testnet";
        }
        
        [TestMethod]
        public void Constructor_ThrowsException_NullBlockFinalizer()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(null, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object)
                );

            Assert.AreEqual("blockFinalizer", ex.ParamName);
        }


        [TestMethod]
        public void Constructor_ThrowsException_NullTransactionValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(_blockFinalizerMock.Object, null, _timestamperMock.Object, _signerMock.Object)
                );

            Assert.AreEqual("transactionValidator", ex.ParamName);
        }

        [TestMethod]
        public void Constructor_ThrowsException_NullTimestamper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, null, _signerMock.Object)
                );

            Assert.AreEqual("timestamper", ex.ParamName);
        }

        [TestMethod]
        public void Constructor_ThrowsException_NullSigner()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, null)
                );

            Assert.AreEqual("signer", ex.ParamName);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_NotFinalized()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>()); // Block is not finalized
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Block is not hashed or signed or hashed properly", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_EmptyBlockHash()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize("", null);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Block is not hashed or signed or hashed properly", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_EmptySignature()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize("hash", "");
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Block is not hashed or signed or hashed properly", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_NullSignature()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize("hash", null);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Block is not hashed or signed or hashed properly", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        /// <summary>
        /// Every Block object contains a 'Hash' property.
        /// When the validator receives a block and the 'setBlockHash' parameter is false,
        /// the hash will be caculated and must match the 'Hash' property from the object.
        /// This test covers the scenario where the hash property from the object differs
        /// from the actual hash calculation.
        /// </summary>
        [TestMethod]
        public void BlockIsValid_ThrowsException_HashDoesNotEqual()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize("abc", "signature"); // Invalid block hash
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns("otherhash");
            
            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("The hash property of the block is not equal to the calculated hash", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        /// <summary>
        /// When the hash value is correct when the first character begins with a zero,
        /// indicating it is a positive number, only when it is lower or equal than the given target.
        /// This is an integration test because it involves the result of BlockHeaderHelper.GetBlockHeaderBytes().
        /// </summary>
        [TestMethod]
        public void BlockIsValid_ThrowsException_HashHasNoLeadingZero()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockHash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>()); // The first SHA attempt results in a value that is higher than the currentTarget
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Hash has no leading zero", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }
        
        [TestMethod]
        public void BlockIsValid_ThrowsException_HashHasHighValueWithLeadingZero()
        {
            var blockHash = "0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"; // High hash value
            BigDecimal currentTarget = BigInteger.Parse("0000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Hash value is equal or higher than the current target", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_HashValueEqualsTargetWithLeadingZero()
        {
            // The currentTarget is the exact same hash value as the blockToTest header.
            var blockHash = "078ECE2577907E270349C3FD60F1B1B28B233BE6DC936C2415624E65C6159E1E";
            BigDecimal currentTarget = BigInteger.Parse(blockHash, NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature"); // The same as target
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Hash value is equal or higher than the current target", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_TimestampIsTooEarly()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(BlockchainConstants.MaximumTimestampOffset + 10);
            // The blockToTest's timestamp is too early because the current timestamp is 130 and the block is from timestamp 1

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        /// <summary>
        /// Testing if the timestamp is at the earliest value possible, but within range.
        /// </summary>
        [TestMethod]
        public void BlockIsValid_EarlyTimestampEdge()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(BlockchainConstants.MaximumTimestampOffset + 1); // The blockToTest's timestamp is exactly on the edge

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_TimestampIsTooLate()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", BlockchainConstants.MaximumTimestampOffset+10, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // The blockToTest's timestamp is too late

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        /// <summary>
        /// Testing if the timestamp is at the latest value possible, but within range.
        /// </summary>
        [TestMethod]
        public void BlockIsValid_LateTimestampEdge()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", BlockchainConstants.MaximumTimestampOffset+1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // The blockToTest's timestamp is exactly on the edge

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreNotEqual("Hash has no leading zero", ex.Message);
            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_TimestampWithinRange()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 15, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // Exact same timestamp as the block

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_EmptyTransactionList()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "abc", 1, ""), new List<AbstractTransaction>());
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            
            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );
            
            Assert.AreEqual("Transaction list cannot be empty", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_IncorrectMerkleRoot()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction("from", "tp", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 1)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("otherMerklerootValue");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Incorrect merkleroot", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_FirstTransactionNotCoinbase()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction("from", "tp", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 1)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("First transaction is not coinbase", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_MultipleCoinbaseTransactions()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0),
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain(_netId), true, true)
                );

            Assert.AreEqual("Multiple coinbase transactions found", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_DifferentNetwork()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader("network1", 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, new Blockchain("network2"), true, true)
                );

            Assert.AreEqual("Block comes from a different network", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }
        
        [TestMethod]
        public void BlockIsValid_ThrowsException_InvalidSignature()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockchain = new Blockchain(new List<Block>() { new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions) }, _netId);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(false); // validation returns false
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true)
                );

            Assert.AreEqual("Block's signature is invalid", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_PreviousHashDoesNotExist()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockchain = new Blockchain(new List<Block>() { new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions) }, _netId);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true)
                );

            Assert.AreEqual("Previous blockhash does not exist in our chain", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_RethrowsTransactionRejected()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var expectedException = new TransactionRejectedException("Transaction rejected test", transactions.First());
            var blockchain = new Blockchain(new List<Block>() { new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "").Finalize("block1", "privkey"), transactions) }, _netId);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "block1"), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First())).Throws(expectedException);

            var ex = Assert.ThrowsException<TransactionRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true)
                );

            Assert.AreEqual(expectedException, ex);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_ChainSplitting()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockchain = new Blockchain(new List<Block>() {
                new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "").Finalize("block1", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 3, "block1").Finalize("block2", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 10, "block2").Finalize("block3", "privkey"), transactions)
            }, _netId);
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 19, "block1"), transactions);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First()));

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true)
                );

            Assert.AreEqual("Chain splitting is not supported", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_ExistingBlockWithHigherDifficulty()
        {
            var lowerBlockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            var highBlockHash = "00000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 16, "block2"), transactions); // Also point to block 2

            var blockchain = new Blockchain(new List<Block>() {
                new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "").Finalize("block1", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 3, "block1").Finalize("block2", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 10, "block2").Finalize(highBlockHash, "privkey"), transactions)
            }, _netId);
            blockToTest.Header.Finalize(lowerBlockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(lowerBlockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", lowerBlockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First()));

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true)
                );

            Assert.AreEqual("Another block with higher difficulty points to the same PreviousHash", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_FollowsHappyFlowForGenesis()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, ""), transactions);
            var blockchain = new Blockchain(_netId);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First()));

            sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true);
            
            Assert.AreEqual(blockToTest, blockchain.Blocks.First());
            Assert.AreEqual(1, blockchain.Blocks.Count());
        }

        [TestMethod]
        public void BlockIsValid_FollowsHappyFlowAndReplaceLastBlock()
        {
            var lastBlockHash =     "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            var betterBlockHash =   "00000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 16, "block2"), transactions); // Also point to block 2

            var blockchain = new Blockchain(new List<Block>() {
                new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "").Finalize("block1", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 3, "block1").Finalize("block2", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 10, "block2").Finalize(lastBlockHash, "privkey"), transactions)
            }, _netId);
            blockToTest.Header.Finalize(betterBlockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(betterBlockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", betterBlockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First()));

            sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true);

            Assert.AreEqual(blockToTest, blockchain.Blocks.Last());
            Assert.AreEqual(3, blockchain.Blocks.Count());
        }

        [TestMethod]
        public void BlockIsValid_FollowsHappyFlow()
        {
            var blockHash = "000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockFinalizerMock.Object, _transactionValidatorMock.Object, _timestamperMock.Object, _signerMock.Object);
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockToTest = new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "block3"), transactions);

            var blockchain = new Blockchain(new List<Block>() {
                new Block(new BlockHeader(_netId, 1, "merkleroot", 1, "").Finalize("block1", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 3, "block1").Finalize("block2", "privkey"), transactions),
                new Block(new BlockHeader(_netId, 1, "merkleroot", 10, "block2").Finalize("block3", "privkey"), transactions)
            }, _netId);
            blockToTest.Header.Finalize(blockHash, "signature");
            _blockFinalizerMock.Setup(m => m.CalculateHash(blockToTest)).Returns(blockHash);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);
            _signerMock.Setup(m => m.SignatureIsValid("signature", blockHash, "to")).Returns(true);
            _transactionValidatorMock.Setup(m => m.CalculateMerkleRoot(transactions)).Returns("merkleroot");
            _transactionValidatorMock.Setup(m => m.ValidateTransaction(transactions.First()));

            sut.ValidateBlock(blockToTest, currentTarget, blockchain, true, true);

            Assert.AreEqual(blockToTest, blockchain.Blocks.Last());
            Assert.AreEqual(4, blockchain.Blocks.Count());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _timestamperMock.VerifyAll();
            _blockFinalizerMock.VerifyAll();
            _transactionValidatorMock.VerifyAll();
            _difficultyCalculatorMock.VerifyAll();
            _signerMock.VerifyAll();
        }
    }
}
