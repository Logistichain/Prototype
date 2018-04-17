using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Mpb.Consensus.BlockLogic;
using Mpb.Consensus.MiscLogic;
using Mpb.Consensus.TransactionLogic;
using Mpb.Shared;
using Mpb.Model;
using Mpb.Consensus.Exceptions;

namespace Mpb.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class PowBlockValidatorTest
    {
        Mock<BlockHeaderHelper> _blockHeaderHelper; // Using implementation because some tests require calling the base.
        Mock<ITimestamper> _timestamper;
        Mock<ITransactionValidator> _transactionValidator;

        [TestInitialize]
        public void Initialize()
        {
            _blockHeaderHelper = new Mock<BlockHeaderHelper>(MockBehavior.Strict);
            _timestamper = new Mock<ITimestamper>(MockBehavior.Strict);
            _transactionValidator = new Mock<ITransactionValidator>(MockBehavior.Strict);
        }
        
        [TestMethod]
        public void Constructor_ThrowsException_NullBlockHeaderHelper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(null, _transactionValidator.Object, _timestamper.Object)
                );

            Assert.AreEqual("blockHeaderHelper", ex.ParamName);
        }


        [TestMethod]
        public void Constructor_ThrowsException_NullTransactionValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(_blockHeaderHelper.Object, null, _timestamper.Object)
                );

            Assert.AreEqual("transactionValidator", ex.ParamName);
        }

        [TestMethod]
        public void Constructor_ThrowsException_NullTimestamper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, null)
                );

            Assert.AreEqual("timestamper", ex.ParamName);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_NullBlockHash()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>()); // Hash is not set right now.
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);

            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, false)
                );

            Assert.AreEqual("Hash", ex.ParamName);
        }


        /// <summary>
        /// This test checks if the validator caluclates the correct hash
        /// from the blockheaderhelper output.
        /// This test asserts on an exception because the block hash does not
        /// have a leading zero, but atleast it should not give an exception
        /// on the hash property.
        /// </summary>
        [TestMethod]
        public void BlockIsValid_Calls_BlockHeaderHelper_AndCalculatesCorrectHash()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            var blockHeaderResult = new byte[] { 0, 1 };
            var expectedHash = "";
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).Returns(blockHeaderResult);
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(blockHeaderResult);
                expectedHash = BitConverter.ToString(hash).Replace("-", "");
            }

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreEqual(expectedHash, ex.Block.Hash);
            Assert.AreNotEqual("The hash property of the block is not equal to the calculated hash", ex.Message);
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
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.SetHash("abc"); // Invalid block hash
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).Returns(new byte[] { 0, 1 });
            
            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, false)
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
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>()); // The first SHA attempt results in a value that is higher than the currentTarget
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase(); // By calling the base, means this isn't a unittest anymore, but the method call is tested in another test.
            
            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreEqual("Hash has no leading zero", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }
        
        [TestMethod]
        public void BlockIsValid_ThrowsException_HashHasHighValueWithLeadingZero()
        {
            BigDecimal currentTarget = BigInteger.Parse("0000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(3); // The third nonce results in a leading '0'.
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreEqual("Hash value is equal or higher than the current target", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_HashValueEqualsTargetWithLeadingZero()
        {
            // The currentTarget is the exact same hash value as the blockToTest header.
            BigDecimal currentTarget = BigInteger.Parse("078ECE2577907E270349C3FD60F1B1B28B233BE6DC936C2415624E65C6159E1E", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(3);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreEqual("Hash value is equal or higher than the current target", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_TimestampIsTooEarly()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(3);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(130); // The blockToTest's timestamp is too early

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
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
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(3);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(121); // The blockToTest's timestamp is exactly on the edge

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_TimestampIsTooLate()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 130, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(152);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // The blockToTest's timestamp is too late

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
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
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 121, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(6);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // The blockToTest's timestamp is exactly on the edge

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreNotEqual("Hash has no leading zero", ex.Message);
            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_TimestampWithinRange()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 15, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(8);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1);

            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );

            Assert.AreNotEqual("Timestamp is not within the acceptable time range", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_ThrowsException_EmptyTransactionList()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>());
            blockToTest.IncrementNonce(3);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // Exact same timestamp as the block


            var ex = Assert.ThrowsException<BlockRejectedException>(
                    () => sut.ValidateBlock(blockToTest, currentTarget, true)
                );
            
            Assert.AreEqual("Transaction list cannot be empty", ex.Message);
            Assert.AreEqual(blockToTest, ex.Block);
        }

        [TestMethod]
        public void BlockIsValid_FollowsHappyFlow()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object, _transactionValidator.Object, _timestamper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<AbstractTransaction>() { null });
            blockToTest.IncrementNonce(8);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();
            _timestamper.Setup(m => m.GetCurrentUtcTimestamp()).Returns(1); // Exact same timestamp as the block

            try
            {
                sut.ValidateBlock(blockToTest, currentTarget, true);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _timestamper.VerifyAll();
            _blockHeaderHelper.VerifyAll();
            _transactionValidator.VerifyAll();
        }
    }
}
