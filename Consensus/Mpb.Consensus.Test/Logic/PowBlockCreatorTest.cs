using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.MiscLogic;
using System.Numerics;
using Mpb.Consensus.Model;
using System.Globalization;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Contract;
using System.Collections.Generic;

namespace Mpb.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class PowBlockCreatorTest
    {
        Mock<BlockHeaderHelper> _blockHeaderHelper;
        Mock<PowBlockValidator> _validatorMock;
        Mock<ITimestamper> _timestamperMock;
        BigDecimal _maximumTarget;
        string _netId;
        int _protocol;
        IEnumerable<Transaction> _transactions;

        [TestInitialize]
        public void Initialize()
        {
            _blockHeaderHelper = new Mock<BlockHeaderHelper>(MockBehavior.Strict);
            _validatorMock = new Mock<PowBlockValidator>(MockBehavior.Strict, new object[] { _blockHeaderHelper.Object });
            _timestamperMock = new Mock<ITimestamper>(MockBehavior.Strict);
            _maximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            _netId = "testnet";
            _protocol = 1;
            _transactions = new List<Transaction>();
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullTimestamper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(null, _validatorMock.Object, _blockHeaderHelper.Object)
                );

            Assert.AreEqual(ex.ParamName, "timestamper");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, null, _blockHeaderHelper.Object)
                );

            Assert.AreEqual(ex.ParamName, "validator");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullBlockHeaderHelper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, _validatorMock.Object, null)
                );

            Assert.AreEqual(ex.ParamName, "blockHeaderHelper");
        }

        /// <summary>
        /// The 'bare' overload of the CreateValidBlock method uses BlockchainConstants values.
        /// We can't access those values from our assembly so we will copy those values.
        /// Once a BlockchainConstants value changes, this test will fail. That means you will
        /// need to check all custom parameters in other projects which defer from the usual consensus rules!
        /// </summary>
        [TestMethod]
        public void CreateValidBlockOverload_Uses_ConstantValues()
        {
            BigDecimal difficulty = 1;
            string expectedNetworkIdentifier = "testnet";
            int expectedProtocolVersion = 1;
            BigDecimal expectedMaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var expectedBlock = new Block(expectedNetworkIdentifier, expectedProtocolVersion, "abc", 123, _transactions);
            var selfCallingMock = new Mock<PowBlockCreator>(MockBehavior.Strict, new object[] { _timestamperMock.Object, _validatorMock.Object, _blockHeaderHelper.Object }) { CallBase = true };
            selfCallingMock.Setup(m => m.CreateValidBlock(_transactions, difficulty)).CallBase();
            selfCallingMock.Setup(m => m.CreateValidBlock(expectedNetworkIdentifier, expectedProtocolVersion, _transactions, difficulty, expectedMaximumTarget))
                .Returns(expectedBlock);
            PowBlockCreator sut = selfCallingMock.Object;

            var result = sut.CreateValidBlock(_transactions, difficulty);

            Assert.AreEqual(expectedBlock, result);
            selfCallingMock.VerifyAll();
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NegativeDifficulty()
        {
            CreateValidBlockThatShouldThrowExceptionOnInvalidDifficulty(-2);
        }

        /// <summary>
        /// We don't like dividing with 0.
        /// </summary>
        [TestMethod]
        public void CreateValidBlock_ThrowsException_ZeroDifficulty()
        {
            CreateValidBlockThatShouldThrowExceptionOnInvalidDifficulty(0);
        }

        private void CreateValidBlockThatShouldThrowExceptionOnInvalidDifficulty(BigDecimal difficulty)
        {
            var expectedExceptionMessage = "Difficulty cannot be zero.";
            var sut = new PowBlockCreator(_timestamperMock.Object, _validatorMock.Object, _blockHeaderHelper.Object);

            var ex = Assert.ThrowsException<DifficultyCalculationException>(
                    () => sut.CreateValidBlock(_netId, _protocol, _transactions, difficulty, _maximumTarget)
                );

            Assert.AreEqual(expectedExceptionMessage, ex.Message);
        }

        [TestMethod]
        public void CreateValidBlock_Calls_Validator()
        {
            var expectedTimestamp = 1;
            var sut = new PowBlockCreator(_timestamperMock.Object, _validatorMock.Object, _blockHeaderHelper.Object);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp())
                            .Returns(expectedTimestamp);
            _validatorMock.Setup(m => m.BlockIsValid(It.IsAny<Block>(), It.IsAny<BigDecimal>(), It.IsAny<byte[]>()))
                          .Returns(true);
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(It.IsAny<Block>()))
                          .Returns(new byte[] { });
            var result = sut.CreateValidBlock(_netId, _protocol, _transactions, 1, _maximumTarget);

            Assert.AreEqual(_netId, result.MagicNumber);
            Assert.AreEqual(_protocol, result.Version);
            Assert.AreEqual(expectedTimestamp, result.Timestamp);
            Assert.AreEqual("abc", result.MerkleRoot);
            Assert.AreEqual(_transactions, result.Transactions);
            Assert.AreEqual(1UL, result.Nonce);
            _validatorMock.VerifyAll();
            _timestamperMock.VerifyAll();
            _blockHeaderHelper.VerifyAll();
        }
    }
}
