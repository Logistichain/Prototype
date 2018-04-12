using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.Constants;
using Mpb.Consensus.Logic.DAL;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Logic.MiscLogic;
using Mpb.Consensus.Logic.TransactionLogic;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Test.Logic.StateTransactionValidators
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class TransferTokenTransactionValidatorTest
    {
        Mock<ITimestamper> _timestamper;
        Mock<IBlockchainRepository> _blockchainRepoMock;
        Mock<ITransactionRepository> _transactionRepoMock;
        Mock<ISkuRepository> _skuRepoMock;
        Mock<ITransactionFinalizer> _transactionFinalizer;
        string _netid;

        [TestInitialize]
        public void Initialize()
        {
            _timestamper = new Mock<ITimestamper>(MockBehavior.Strict);
            _blockchainRepoMock = new Mock<IBlockchainRepository>(MockBehavior.Strict);
            _transactionRepoMock = new Mock<ITransactionRepository>(MockBehavior.Strict);
            _skuRepoMock = new Mock<ISkuRepository>(MockBehavior.Strict);
            _transactionFinalizer = new Mock<ITransactionFinalizer>(MockBehavior.Strict);
            _netid = "testnet"; // This value is not coupled to the BlockchainConstants.cs value
        }

        [TestMethod]
        public void TransferTokenValidateTransaction_ThrowsException_NullFromPubKey()
        {
            var expectedTransaction = new StateTransaction(null, "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);
            _transactionFinalizer.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");
            _transactionFinalizer.Setup(m => m.CreateSignature(It.IsAny<AbstractTransaction>())).Returns("");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.FromPubKey) + " field cannot be null", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TransferTokenValidateTransaction_ThrowsException_NullToPubKey()
        {
            var expectedTransaction = new StateTransaction("from", null, null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);
            _transactionFinalizer.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");
            _transactionFinalizer.Setup(m => m.CreateSignature(It.IsAny<AbstractTransaction>())).Returns("");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.ToPubKey) + " field cannot be null", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _timestamper.VerifyAll();
            _blockchainRepoMock.VerifyAll();
            _transactionRepoMock.VerifyAll();
            _skuRepoMock.VerifyAll();
            _transactionFinalizer.VerifyAll();
        }
    }
}
