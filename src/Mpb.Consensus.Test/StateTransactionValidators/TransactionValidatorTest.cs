using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Test.Mocks;
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
    public class TransactionValidatorTest
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
        public void ValidateTransaction_Calls_WithDefaultParams()
        {
            var consensusNetworkIdentifier = "testnet"; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction("a", "b", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            var selfCallingMock = new Mock<StateTransactionValidator>(new object[] { _transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object }) { CallBase = true };
            selfCallingMock.Setup(m => m.ValidateTransaction(expectedTransaction, consensusNetworkIdentifier));
            StateTransactionValidator sut = selfCallingMock.Object;

            sut.ValidateTransaction(expectedTransaction);

            selfCallingMock.Verify(m => m.ValidateTransaction(expectedTransaction));
            selfCallingMock.Verify(m => m.ValidateTransaction(expectedTransaction, consensusNetworkIdentifier));
            selfCallingMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_InvalidTransactionType()
        {
            var expectedTransaction = new InvalidTransactionType(1, "blabla action", "Data", 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);

            var exception = Assert.ThrowsException<ArgumentException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Transaction is not of type StateTransaction.", exception.Message);
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_LowerTransactionVersion()
        {
            uint consensusTransactionVersion = 1; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, consensusTransactionVersion - 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Unsupported transaction version", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void ValidateTransaction_ThrowsException_HigherTransactionVersion()
        {
            uint consensusTransactionVersion = 1; //! change this whenever the networkidentifier changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, consensusTransactionVersion + 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Unsupported transaction version", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_NullSenderAndReceiver()
        {
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual(nameof(expectedTransaction.FromPubKey) + " and " + nameof(expectedTransaction.ToPubKey) + " are both null or empty", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_NotFinalized()
        {
            var expectedTransaction = new StateTransaction("from", "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Transaction is not finalized", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_IncorrectHash()
        {
            var expectedTransaction = new StateTransaction("from", "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("invalidhash", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual(nameof(expectedTransaction.Hash) + " is incorrect", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TransferTokenValidateTransaction_ThrowsException_NullFromPubKey()
        {
            var expectedTransaction = new StateTransaction(null, "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 0);
            expectedTransaction.Finalize("hash", "sig");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("hash");

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Tried to validate a signature with an empty public key", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void TokenValidateTransaction_ThrowsException_IncorrectSignature()
        {
            var hash = "hash";
            var senderPublicKey = "from";
            var signature = "sig";
            var expectedTransaction = new StateTransaction(senderPublicKey, "to", null, 0, 1, 1, TransactionAction.TransferToken.ToString(), null, 100);
            expectedTransaction.Finalize(hash, signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns(hash);
            _signerMock.Setup(m => m.SignatureIsValid(signature, hash, senderPublicKey)).Returns(false);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Transaction signature is incorrect", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        // todo check transaction signature call

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
