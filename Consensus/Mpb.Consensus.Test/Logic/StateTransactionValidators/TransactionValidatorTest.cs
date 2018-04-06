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
using Mpb.Consensus.Test.Mocks;

namespace Mpb.Consensus.Test.Logic.StateTransactionValidators
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class TransactionValidatorTest
    {
        Mock<ITimestamper> _timestamper;
        Mock<IBlockchainRepository> _blockchainRepoMock;
        Mock<ITransactionRepository> _transactionRepoMock;
        Mock<ISkuRepository> _skuRepoMock;
        Mock<TransactionFinalizer> _transactionFinalizer;
        string _netid;

        [TestInitialize]
        public void Initialize()
        {
            _timestamper = new Mock<ITimestamper>(MockBehavior.Strict);
            _blockchainRepoMock = new Mock<IBlockchainRepository>(MockBehavior.Strict);
            _transactionRepoMock = new Mock<ITransactionRepository>(MockBehavior.Strict);
            _skuRepoMock = new Mock<ISkuRepository>(MockBehavior.Strict);
            _transactionFinalizer = new Mock<TransactionFinalizer>(MockBehavior.Strict);
            _netid = "testnet"; // This value is not coupled to the BlockchainConstants.cs value
        }

        [TestMethod]
        public void ValidateTransaction_Calls_WithDefaultParams()
        {
            var consensusNetworkIdentifier = "testnet"; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction("a", "b", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            var selfCallingMock = new Mock<StateTransactionValidator>(new object[] { _transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object }) { CallBase = true };
            selfCallingMock.Setup(m => m.ValidateTransaction(expectedTransaction, consensusNetworkIdentifier, true));
            StateTransactionValidator sut = selfCallingMock.Object;

            sut.ValidateTransaction(expectedTransaction);

            selfCallingMock.Verify(m => m.ValidateTransaction(expectedTransaction));
            selfCallingMock.Verify(m => m.ValidateTransaction(expectedTransaction, consensusNetworkIdentifier, true));
            selfCallingMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_InvalidTransactionType()
        {
            var expectedTransaction = new InvalidTransactionType(1, "blabla action", "Data", 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<ArgumentException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Transaction is not of type StateTransaction.", exception.Message);
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_LowerTransactionVersion()
        {
            uint consensusTransactionVersion = 1; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, consensusTransactionVersion - 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Unsupported transaction version", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_HigherTransactionVersion()
        {
            uint consensusTransactionVersion = 1; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, consensusTransactionVersion + 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Unsupported transaction version", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }
        
        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_NotFinalized()
        {
            var expectedTransaction = new StateTransaction("from", "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Transaction is not finalized", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_IncorrectHash()
        {
            var expectedTransaction = new StateTransaction("from", "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("invalidhash", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);
            _transactionFinalizer.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.Hash) + " is incorrect", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_IncorrectSignature()
        {
            var expectedTransaction = new StateTransaction("from", "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("", "invalidsignature");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);
            _transactionFinalizer.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");
            _transactionFinalizer.Setup(m => m.CalculateSignature(It.IsAny<AbstractTransaction>())).Returns("");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.Signature) + " is incorrect", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        // todo check transaction signature call

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
