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

namespace Mpb.Consensus.Test
{
    [TestClass]
    public class PowBlockCreatorTest
    {
        Mock<PowBlockValidator> _validatorMock = new Mock<PowBlockValidator>(MockBehavior.Strict);
        Mock<ITimestamper> _timestamperMock = new Mock<ITimestamper>(MockBehavior.Strict);
        BigDecimal _maximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
        string _netId = "testnet";
        int _protocol = 1;
        IEnumerable<Transaction> _transactions = new List<Transaction>();

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullTimestamper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(null, _validatorMock.Object)
                );

            Assert.AreEqual(ex.ParamName, "timestamper");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, null)
                );

            Assert.AreEqual(ex.ParamName, "validator");
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
            var mock = new Mock<PowBlockCreator>(MockBehavior.Strict, new object[] { _timestamperMock.Object, _validatorMock.Object }) { CallBase = true };
            mock.Setup(m => m.CreateValidBlock(_transactions, difficulty)).CallBase();
            mock.Setup(m => m.CreateValidBlock(expectedNetworkIdentifier, expectedProtocolVersion, _transactions, difficulty, expectedMaximumTarget))
                .Returns(expectedBlock);
            PowBlockCreator sut = mock.Object;

            var result = sut.CreateValidBlock(_transactions, difficulty);

            Assert.AreEqual(expectedBlock, result);
            mock.VerifyAll();
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
            var sut = new PowBlockCreator(_timestamperMock.Object, _validatorMock.Object);

            var ex = Assert.ThrowsException<DifficultyCalculationException>(
                    () => sut.CreateValidBlock(_netId, _protocol, _transactions, difficulty, _maximumTarget)
                );

            Assert.AreEqual(ex.Message, expectedExceptionMessage);
        }

        [TestMethod]
        public void CreateValidBlock_Calls_Validator_AndCalls_Validator()
        {
            var expectedTimestamp = 1;
            var sut = new PowBlockCreator(_timestamperMock.Object, _validatorMock.Object);
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp())
                            .Returns(expectedTimestamp);
            _validatorMock.Setup(m => m.BlockIsValid(It.IsAny<Block>(), It.IsAny<BigDecimal>(), It.IsAny<byte[]>()))
                          .Returns(true);
            var result = sut.CreateValidBlock(_netId, _protocol, _transactions, 1, _maximumTarget);

            Assert.AreEqual(result.MagicNumber, _netId);
            Assert.AreEqual(result.Version, _protocol);
            Assert.AreEqual(result.Timestamp, expectedTimestamp);
            Assert.AreEqual(result.MerkleRoot, "abc");
            Assert.AreEqual(result.Transactions, _transactions);
            Assert.AreEqual(result.Nonce, 1UL);
            _validatorMock.VerifyAll();
            _timestamperMock.VerifyAll();
        }
    }
}
