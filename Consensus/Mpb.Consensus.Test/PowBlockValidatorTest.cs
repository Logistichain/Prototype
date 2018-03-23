using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using System.Numerics;
using Mpb.Consensus.Model;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.Test
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class PowBlockValidatorTest
    {
        Mock<BlockHeaderHelper> _blockHeaderHelper;
        SHA256 _sha256;

        [TestInitialize]
        public void Initialize()
        {
            _blockHeaderHelper = new Mock<BlockHeaderHelper>(MockBehavior.Strict);
            _sha256 = SHA256.Create();
        }
        
        [TestMethod]
        public void BlockIsValid_ThrowsException_NullBlockHeaderHelper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockValidator(null)
                );

            Assert.AreEqual("blockHeaderHelper", ex.ParamName);
        }

        /// <summary>
        /// When the hash value is correct, only when it is lower or equal than the given target
        /// </summary>
        [TestMethod]
        public void BlockIsValidOverload_Calls_BlockHeaderHelper()
        {
            var expectedBlockHash = _sha256.ComputeHash(new byte[] { });
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<Transaction>());
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).Returns(new byte[] { });
            var selfCallingMock = new Mock<PowBlockValidator>(MockBehavior.Strict, new object[] { _blockHeaderHelper.Object }) { CallBase = true };
            selfCallingMock.Setup(m => m.BlockIsValid(blockToTest, currentTarget)).CallBase();
            selfCallingMock.Setup(m => m.BlockIsValid(blockToTest, currentTarget, expectedBlockHash)).Returns(true);
            var sut = selfCallingMock.Object;

            var result = sut.BlockIsValid(blockToTest, currentTarget);

            Assert.IsTrue(result);
            selfCallingMock.VerifyAll();
            _blockHeaderHelper.VerifyAll();
        }

        /// <summary>
        /// When the hash value is correct when the first character begins with a zero,
        /// indicating it is a positive number, only when it is lower or equal than the given target.
        /// This is an integration test because it involves the result of BlockHeaderHelper.GetBlockHeaderBytes().
        /// </summary>
        [TestMethod]
        public void BlockIsValid_NoLeadingZero()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<Transaction>()); // The first SHA attempt results in a value that is higher than the currentTarget
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase(); // By calling the base, means this isn't a unittest anymore, but the method call is tested in another test.

            var result = sut.BlockIsValid(blockToTest, currentTarget);

            Assert.IsFalse(result);
            _blockHeaderHelper.VerifyAll();
        }
        
        [TestMethod]
        public void BlockIsValid_HighHashValueWithLeadingZero()
        {
            BigDecimal currentTarget = BigInteger.Parse("0000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<Transaction>());
            blockToTest.IncrementNonce();
            blockToTest.IncrementNonce();
            blockToTest.IncrementNonce(); // The third nonce results in a leading '0'.
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();

            var result = sut.BlockIsValid(blockToTest, currentTarget);

            Assert.IsFalse(result);
            _blockHeaderHelper.VerifyAll();
        }
        
        [TestMethod]
        public void BlockIsValid_LowHashValueWithLeadingZero()
        {
            BigDecimal currentTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var sut = new PowBlockValidator(_blockHeaderHelper.Object);
            var blockToTest = new Block("testnet", 1, "abc", 1, new List<Transaction>());
            blockToTest.IncrementNonce();
            blockToTest.IncrementNonce();
            blockToTest.IncrementNonce(); // The third nonce results in a leading '0'.
            _blockHeaderHelper.Setup(m => m.GetBlockHeaderBytes(blockToTest)).CallBase();

            var result = sut.BlockIsValid(blockToTest, currentTarget);

            Assert.IsTrue(result);
            _blockHeaderHelper.VerifyAll();
        }
    }
}
