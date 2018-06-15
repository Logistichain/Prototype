using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.DAL;
using Mpb.Consensus.MiscLogic;
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
    public class CoinbaseTransactionValidatorTest
    {
        Mock<ITimestamper> _timestamperMock;
        Mock<IBlockchainRepository> _blockchainRepoMock;
        Mock<ITransactionRepository> _transactionRepoMock;
        Mock<ISkuRepository> _skuRepoMock;
        Mock<ITransactionFinalizer> _transactionFinalizerMock;
        Mock<ISigner> _signerMock;
        string _hash = "hash";
        string _signature = "sig";
        string _toPubKey = "miner";
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
        public void CoinbaseValidateTransaction_ThrowsException_NotNullFromPubKey()
        {
            var expectedTransaction = new StateTransaction("wrongvalue", "b", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize(_hash, _signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid(_signature, _hash, "b")).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(expectedTransaction)).Returns(_hash);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual(nameof(expectedTransaction.FromPubKey) + " field must be null in a Coinbase transaction", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_FeeNotZero()
        {
            var expectedTransaction = new StateTransaction(null, _toPubKey, null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 10);
            expectedTransaction.Finalize(_hash, _signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid(_signature, _hash, _toPubKey)).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(expectedTransaction)).Returns(_hash);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Fee must be zero on Coinbase transactions", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_CoinbaseRewardTooHigh()
        {
            uint consensusCoinbaseReward = 5000; //! change this whenever the coinbasereward changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, _toPubKey, null, 0, consensusCoinbaseReward + 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize(_hash, _signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid(_signature, _hash, _toPubKey)).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(expectedTransaction)).Returns(_hash);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid));

            Assert.AreEqual("Coinbase reward is too high. Maximum: " + consensusCoinbaseReward, exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        /// <summary>
        /// There should not be any exceptions if we decide to ignore the coinbase reward
        /// </summary>
        [TestMethod]
        public void CoinbaseValidateTransaction_Successful_ZeroCoinbaseReward()
        {
            var expectedTransaction = new StateTransaction(null, _toPubKey, null, 0, 0, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize(_hash, _signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid(_signature, _hash, _toPubKey)).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(expectedTransaction)).Returns(_hash);

            sut.ValidateTransaction(expectedTransaction, _netid);

            // Should not throw exception
        }

        /// <summary>
        /// Test the happy flow by claiming the maximum coinbase reward, with all other valid fields
        /// </summary>
        [TestMethod]
        public void CoinbaseValidateTransaction_Successful_ExactCoinbaseReward()
        {
            uint consensusCoinbaseReward = 5000; //! change this whenever the coinbasereward changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, _toPubKey, null, 0, consensusCoinbaseReward, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize(_hash, _signature);
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizerMock.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object, _signerMock.Object);
            _signerMock.Setup(m => m.SignatureIsValid(_signature, _hash, _toPubKey)).Returns(true);
            _transactionFinalizerMock.Setup(m => m.CalculateHash(expectedTransaction)).Returns(_hash);

            sut.ValidateTransaction(expectedTransaction, _netid);

            // Should not throw exception
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
