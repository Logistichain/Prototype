using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.MiscLogic;
using Mpb.DAL;
using Mpb.Consensus.BlockLogic;
using Mpb.Model;
using Mpb.Shared.Constants;
using Mpb.Consensus.TransactionLogic;
using Mpb.Consensus.Exceptions;
using Mpb.Consensus.Cryptography;

namespace Mpb.Consensus.Test.Logic.StateTransactionValidators
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class TransferTokenTransactionValidatorTest
    {
        Mock<ITimestamper> _timestamperMock;
        Mock<IBlockchainRepository> _blockchainRepoMock;
        Mock<ITransactionRepository> _transactionRepoMock;
        Mock<ISkuRepository> _skuRepoMock;
        Mock<ITransactionFinalizer> _transactionFinalizerMock;
        Mock<ISigner> _signerMock;
        string _netid;

        [TestInitialize]
        public void Initialize()
        {
            _timestamperMock = new Mock<ITimestamper>(MockBehavior.Strict);
            _blockchainRepoMock = new Mock<IBlockchainRepository>(MockBehavior.Strict);
            _transactionRepoMock = new Mock<ITransactionRepository>(MockBehavior.Strict);
            _skuRepoMock = new Mock<ISkuRepository>(MockBehavior.Strict);
            _transactionFinalizerMock = new Mock<ITransactionFinalizer>(MockBehavior.Strict);
            _signerMock = new Mock<ISigner>(MockBehavior.Strict);
            _netid = "testnet"; // This value is not coupled to the BlockchainConstants.cs value
        }

        [TestMethod]
        public void TransferTokenValidateTransaction_ThrowsException_NullToPubKey()
        {
            var expectedTransaction = new StateTransaction("from", null, null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("hash", "sig");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid("sig", "hash", "from")).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("hash");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual(nameof(expectedTransaction.ToPubKey) + " field cannot be null", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _timestamperMock.VerifyAll();
            _blockchainRepoMock.VerifyAll();
            _transactionRepoMock.VerifyAll();
            _skuRepoMock.VerifyAll();
            _transactionFinalizerMock.VerifyAll();
            _signerMock.VerifyAll();
        }
    }
}
