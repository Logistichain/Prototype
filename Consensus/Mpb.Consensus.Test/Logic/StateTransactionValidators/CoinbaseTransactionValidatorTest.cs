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
    public class CoinbaseTransactionValidatorTest
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

            // Setup transactionfinalizer because it's applied for all tests
            _transactionFinalizer.Setup(m => m.CalculateHash(It.IsAny<AbstractTransaction>())).Returns("");
            _transactionFinalizer.Setup(m => m.CreateSignature(It.IsAny<AbstractTransaction>())).Returns("");
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_NotNullFromPubKey()
        {
            var expectedTransaction = new StateTransaction("wrongvalue", "b", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.FromPubKey) + " field must be null in a Coinbase transaction", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_NotNullToPubKey()
        {
            var expectedTransaction = new StateTransaction(null, null, null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual(nameof(expectedTransaction.ToPubKey) + " field cannot be null", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_FeeNotZero()
        {
            var expectedTransaction = new StateTransaction(null, "miner", null, 0, 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 10);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Fee must be zero on Coinbase transactions", exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        [TestMethod]
        public void CoinbaseValidateTransaction_ThrowsException_CoinbaseRewardTooHigh()
        {
            uint consensusCoinbaseReward = 5000; //! change this whenever the coinbasereward changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, "miner", null, 0, consensusCoinbaseReward + 1, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            var exception = Assert.ThrowsException<TransactionRejectedException>(() => sut.ValidateTransaction(expectedTransaction, _netid, true));

            Assert.AreEqual("Coinbase reward is too high. Maximum: " + consensusCoinbaseReward, exception.Message);
            Assert.AreEqual(expectedTransaction, exception.Transaction);
        }

        /// <summary>
        /// There should not be any exceptions if we decide to ignore the coinbase reward
        /// </summary>
        [TestMethod]
        public void CoinbaseValidateTransaction_Successful_ZeroCoinbaseReward()
        {
            var expectedTransaction = new StateTransaction(null, "miner", null, 0, 0, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            sut.ValidateTransaction(expectedTransaction, _netid, true);

            // Should not throw exception
        }

        /// <summary>
        /// Test the happy flow by claiming the maximum coinbase reward, with all other valid fields
        /// </summary>
        [TestMethod]
        public void CoinbaseValidateTransaction_Successful_ExactCoinbaseReward()
        {
            uint consensusCoinbaseReward = 5000; //! change this whenever the coinbasereward changes in the mpb.consensus assembly!
            var expectedTransaction = new StateTransaction(null, "miner", null, 0, consensusCoinbaseReward, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0);
            expectedTransaction.Finalize("", "");
            StateTransactionValidator sut = new StateTransactionValidator(_transactionFinalizer.Object, _blockchainRepoMock.Object, _transactionRepoMock.Object, _skuRepoMock.Object);

            sut.ValidateTransaction(expectedTransaction, _netid, true);

            // Should not throw exception
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
