using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using Logistichain.Consensus.BlockLogic;
using Logistichain.Consensus.MiscLogic;
using Logistichain.Consensus.TransactionLogic;
using Logistichain.DAL;
using Logistichain.Shared;
using Logistichain.Model;
using Logistichain.Consensus.Exceptions;
using System.Linq;
using Logistichain.Shared.Constants;

namespace Logistichain.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class PowBlockCreatorTest
    {
        // Todo add difficultycalculator mock and create some tests for it
        Mock<IBlockFinalizer> _blockFinalizer;
        Mock<IBlockValidator> _blockValidatorMock;
        Mock<ITimestamper> _timestamperMock;
        Mock<ITransactionValidator> _transactionValidator;
        Mock<ITransactionFinalizer> _transactionFinalizer;
        Mock<ITransactionRepository> _transactionRepo;
        BigDecimal _maximumTarget;
        string _netId;
        uint _protocol;
        Blockchain _blockchain;
        IEnumerable<AbstractTransaction> _transactions;

        [TestInitialize]
        public void Initialize()
        {
            _netId = "testnet";
            _protocol = 1;
            _blockchain = new Blockchain(_netId);
            _transactionFinalizer = new Mock<ITransactionFinalizer>(MockBehavior.Strict);
            _transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
            _blockFinalizer = new Mock<IBlockFinalizer>(MockBehavior.Strict);
            _timestamperMock = new Mock<ITimestamper>(MockBehavior.Strict);
            _transactionValidator = new Mock<ITransactionValidator>(MockBehavior.Strict);
            _blockValidatorMock = new Mock<IBlockValidator>(MockBehavior.Strict);
            _maximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            _transactions = new List<AbstractTransaction>();
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullTimestamper()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(null, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object)
                );

            Assert.AreEqual(ex.ParamName, "timestamper");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, null, _blockFinalizer.Object, _transactionValidator.Object)
                );

            Assert.AreEqual(ex.ParamName, "validator");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullBlockFinalizer()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, null, _transactionValidator.Object)
                );

            Assert.AreEqual(ex.ParamName, "blockFinalizer");
        }

        [TestMethod]
        public void CreateValidBlock_ThrowsException_NullTransactionValidator()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(
                    () => new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, null)
                );

            Assert.AreEqual(ex.ParamName, "transactionValidator");
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
            uint expectedProtocolVersion = 1;
            BigDecimal expectedMaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            var blockchain = new Blockchain(expectedNetworkIdentifier);
            var expectedBlock = new Block(new BlockHeader(expectedNetworkIdentifier, expectedProtocolVersion, "abc", 123, null), _transactions);
            var selfCallingMock = new Mock<PowBlockCreator>(MockBehavior.Strict, new object[] { _timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object });
            selfCallingMock.Setup(m => m.CreateValidBlockAndAddToChain("privkey", blockchain, _transactions, difficulty)).CallBase();
            selfCallingMock.Setup(m => m.CreateValidBlockAndAddToChain("privkey", blockchain, expectedProtocolVersion, _transactions, difficulty, expectedMaximumTarget, CancellationToken.None))
                .Returns(expectedBlock);
            PowBlockCreator sut = selfCallingMock.Object;

            var result = sut.CreateValidBlockAndAddToChain("privkey", blockchain, _transactions, difficulty);

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
            var sut = new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object);

            var ex = Assert.ThrowsException<DifficultyCalculationException>(
                    () => sut.CreateValidBlockAndAddToChain("privkey", new Blockchain(_netId), _protocol, _transactions, difficulty, _maximumTarget, CancellationToken.None)
                );

            Assert.AreEqual(expectedExceptionMessage, ex.Message);
        }

        /// <summary>
        /// By proving that the mining can be canceled at any time, an unrealistically
        /// hard difficulty will be given which is almost impossible to sovle.
        /// </summary>
        [TestMethod]
        public void CreateValidBlock_ThrowsException_MiningCanceled()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var sut = new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object);
            var veryHardDifficulty = BigInteger.Parse("00000000000000000000000000000000000000000000000000000000000000F", NumberStyles.HexNumber);
            var transactionsList = _transactions.ToList();
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp())
                            .Returns(1);
            _transactionValidator.Setup(m => m.CalculateMerkleRoot(transactionsList)).Returns("abc"); ;

            var ex = Assert.ThrowsException<OperationCanceledException>(
                    () =>
                    {
                        cts.Cancel();
                        sut.CreateValidBlockAndAddToChain("privkey", new Blockchain(_netId), _protocol, transactionsList, veryHardDifficulty, _maximumTarget, cts.Token);
                    }
                );
        }

        [TestMethod]
        public void CreateValidBlock_CallsValidator_GenesisHappyFlow()
        {
            var expectedHash = "hash";
            var privateKey = "privkey";
            var expectedTimestamp = 1;
            var blockchain = new Blockchain(_netId);
            var sut = new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object);
            var transactionsList = _transactions.ToList();
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp())
                            .Returns(expectedTimestamp);
            _blockFinalizer.Setup(m => m.CalculateHash(It.IsAny<Block>())).Returns(expectedHash);
            _blockFinalizer.Setup(m => m.FinalizeBlock(It.IsAny<Block>(), expectedHash, privateKey));
            _blockValidatorMock.Setup(m => m.ValidateBlock(It.IsAny<Block>(), It.IsAny<BigDecimal>(), blockchain, true, true));
            _transactionValidator.Setup(m => m.CalculateMerkleRoot(transactionsList)).Returns("abc");
            _blockFinalizer.Setup(m => m.FinalizeBlock(It.IsAny<Block>(), expectedHash, privateKey));

            var result = sut.CreateValidBlockAndAddToChain(privateKey, blockchain, _protocol, transactionsList, 1, _maximumTarget, CancellationToken.None);
            
            Assert.AreEqual(blockchain.NetIdentifier, result.Header.MagicNumber);
            Assert.AreEqual(null, result.Header.PreviousHash);
            Assert.AreEqual(_protocol, result.Header.Version);
            Assert.AreEqual(expectedTimestamp, result.Header.Timestamp);
            Assert.AreEqual("abc", result.Header.MerkleRoot);
            Assert.AreEqual(transactionsList, result.Transactions);
            Assert.AreEqual(1UL, result.Header.Nonce);
        }

        [TestMethod]
        public void CreateValidBlock_CallsValidator_HappyFlow()
        {
            var expectedHash = "hash";
            var privateKey = "privkey";
            var expectedTimestamp = 1;
            var transactions = new List<AbstractTransaction>() {
                new StateTransaction(null, "to", null, 0, 5000, 1, TransactionAction.ClaimCoinbase.ToString(), null, 0)
            };
            var blockchain = new Blockchain(new List<Block>() { new Block(new BlockHeader(_netId, 1, "merkleroot", 1, null).Finalize("firsthash", "sig"), transactions) }, _netId);
            var sut = new PowBlockCreator(_timestamperMock.Object, _blockValidatorMock.Object, _blockFinalizer.Object, _transactionValidator.Object);
            var transactionsList = _transactions.ToList();
            _timestamperMock.Setup(m => m.GetCurrentUtcTimestamp())
                            .Returns(expectedTimestamp);
            _blockFinalizer.Setup(m => m.CalculateHash(It.IsAny<Block>())).Returns(expectedHash);
            _blockFinalizer.Setup(m => m.FinalizeBlock(It.IsAny<Block>(), expectedHash, privateKey));
            _blockValidatorMock.Setup(m => m.ValidateBlock(It.IsAny<Block>(), It.IsAny<BigDecimal>(), blockchain, true, true));
            _transactionValidator.Setup(m => m.CalculateMerkleRoot(transactionsList)).Returns("abc");
            _blockFinalizer.Setup(m => m.FinalizeBlock(It.IsAny<Block>(), expectedHash, privateKey));

            var result = sut.CreateValidBlockAndAddToChain(privateKey, blockchain, _protocol, transactionsList, 1, _maximumTarget, CancellationToken.None);

            Assert.AreEqual(blockchain.NetIdentifier, result.Header.MagicNumber);
            Assert.AreEqual("firsthash", result.Header.PreviousHash);
            Assert.AreEqual(_protocol, result.Header.Version);
            Assert.AreEqual(expectedTimestamp, result.Header.Timestamp);
            Assert.AreEqual("abc", result.Header.MerkleRoot);
            Assert.AreEqual(transactionsList, result.Transactions);
            Assert.AreEqual(1UL, result.Header.Nonce);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transactionFinalizer.VerifyAll();
            _transactionRepo.VerifyAll();
            _blockFinalizer.VerifyAll();
            _timestamperMock.VerifyAll();
            _transactionValidator.VerifyAll();
            _blockValidatorMock.VerifyAll();
        }
    }
}
