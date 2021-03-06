using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Cryptography;
using Logistichain.Consensus.BlockLogic;
using Logistichain.Model;
using Moq;
using Logistichain.Consensus.Cryptography;

namespace Logistichain.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class BlockHeaderHelperTest
    {
        [TestMethod]
        public void GetBlockHeaderBytes_ThrowsException_NullBlock()
        {
            var signerMock = new Mock<ISigner>(MockBehavior.Strict);
            var sut = new PowBlockFinalizer(signerMock.Object);
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => sut.GetBlockHeaderBytes(null)
                );
            
            // We only want to see if the message is correct. We don't care how the method names their params
            Assert.IsTrue(ex.Message.StartsWith("Block cannot be null"));
            signerMock.VerifyAll();
        }
        
        /// <summary>
        /// Sends a dummy block object to GetBlockHeaderBytes method.
        /// The output will be hashed and that hash will be compared.
        /// </summary>
        [TestMethod]
        public void GetBlockHeaderBytes_ReturnsValidHeader()
        {
            var signerMock = new Mock<ISigner>(MockBehavior.Strict);
            var sut = new PowBlockFinalizer(signerMock.Object);
            var expectedHash = "FF5648F9B5FEB7AA0ACECA5AF77938A811B5F18E4D1CC5A806C475E4AFED47EA";

            var blockToTest = new Block(new BlockHeader("testnet", 1, "abc", 1, ""), new List<AbstractTransaction>());

            var bytesResult = sut.GetBlockHeaderBytes(blockToTest);
            var hashString = "";

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytesResult);
                hashString = BitConverter.ToString(hash).Replace("-", ""); // Microsoft's SHA adds dashes. I don't like that :)
            }

            Assert.AreEqual(expectedHash, hashString);
            signerMock.VerifyAll();
        }
    }
}
