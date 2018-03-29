using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using System.Numerics;
using Mpb.Consensus.Model;
using System.Globalization;
using Mpb.Consensus.Logic.Exceptions;
using System.Collections.Generic;

namespace Mpb.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class DifficultyCalculatorTest
    {
        string _netId;
        uint _protocol;

        [TestInitialize]
        public void Initialize()
        {
            _netId = "testnet";
            _protocol = 1;
        }
                
        [TestMethod]
        public void CalculateCurrentDifficulty_Calls_CalculateDifficultyForHeight()
        {
            var expectedHeight = -1;
            BigDecimal expectedDifficulty = 1;
            var selfCallingMock = new Mock<DifficultyCalculator>(MockBehavior.Strict);
            var blockchainMock = new Mock<Blockchain>(MockBehavior.Strict, new object[] { _netId });
            blockchainMock.Setup(m => m.CurrentHeight).Returns(expectedHeight);

            selfCallingMock.Setup(m => m.CalculateCurrentDifficulty(blockchainMock.Object)).CallBase();
            selfCallingMock.Setup(m => m.CalculateDifficultyForHeight(blockchainMock.Object, expectedHeight)).Returns(expectedDifficulty);
            DifficultyCalculator sut = selfCallingMock.Object;

            var result = sut.CalculateCurrentDifficulty(blockchainMock.Object);

            Assert.AreEqual(expectedDifficulty, result);
            selfCallingMock.VerifyAll();
            blockchainMock.VerifyAll();
        }
        
        /// <summary>
        /// The CalculateDifficultyForHeight method uses BlockchainConstants values to call CalculateDifficulty.
        /// We can't access those values from our assembly so we will copy those values.
        /// Once a BlockchainConstants value changes, this test will fail. That means you will
        /// need to check all custom parameters in other projects which defer from the usual consensus rules!
        /// </summary>
        [TestMethod]
        public void CalculateDifficultyForHeight_Uses_ConstantValues()
        {
            uint protocol = 1;
            uint secondsPerBlockGoal = 15;
            int difficultyUpdateCycle = 10;
            var blockchainHeight = -1;
            BigDecimal expectedDifficulty = 1;
            var selfCallingMock = new Mock<DifficultyCalculator>(MockBehavior.Strict);
            var blockchain = new Blockchain(_netId);
            selfCallingMock.Setup(m => m.CalculateDifficultyForHeight(blockchain, blockchainHeight)).CallBase();
            selfCallingMock.Setup(m => m.CalculateDifficulty(blockchain, blockchainHeight, protocol, secondsPerBlockGoal, difficultyUpdateCycle)).Returns(expectedDifficulty);
            DifficultyCalculator sut = selfCallingMock.Object;

            var result = sut.CalculateDifficultyForHeight(blockchain, blockchainHeight);

            Assert.AreEqual(expectedDifficulty, result);
            selfCallingMock.VerifyAll();
        }

        [TestMethod]
        public void CalculateDifficulty_LowHeight_ReturnsOne()
        {
            CalculateDifficultyAndExpectValue(2, "1E0", null);
        }

        [TestMethod]
        public void CalculateDifficulty_ForHeight9_ReturnsOne()
        {
            CalculateDifficultyAndExpectValue(9, "1E0", null);
        }

        [TestMethod]
        public void CalculateDifficulty__For10Blocks_ReturnsCorrectDifficulty()
        {
            var blocks = new List<Block>();
            for (int i = 10; i < 111; i = i + 10)
            {
                blocks.Add(new Block(_netId, _protocol, "abc", i, new List<AbstractTransaction>()));
            }
            CalculateDifficultyAndExpectValue(10, "16666666666666668E-16", blocks);
        }

        [TestMethod]
        public void CalculateDifficulty__For63Blocks_ReturnsCorrectDifficulty()
        {
            var blocks = new List<Block>();
            for (int i = 10; i < 641; i = i + 10)
            {
                blocks.Add(new Block(_netId, _protocol, "abc", i, new List<AbstractTransaction>()));
            }
            CalculateDifficultyAndExpectValue(63, "21433470507544591906721536351168038408779149520109739368998628271056241426611797403566529492455424E-96", blocks);
        }

        private void CalculateDifficultyAndExpectValue(int height, string expectedValue, List<Block> blocks)
        {
            uint secondsPerBlockGoal = 15;
            int difficultyUpdateCycle = 10;
            var blockchain = blocks == null ? new Blockchain(_netId) : new Blockchain(blocks, _netId);
            var sut = new DifficultyCalculator();

            var result = sut.CalculateDifficulty(blockchain, height, _protocol, secondsPerBlockGoal, difficultyUpdateCycle);

            Assert.AreEqual(expectedValue, result.ToString());
        }

        /// <summary>
        /// The 'bare' overload of GetPreviousDifficultyUpdateInformation method uses BlockchainConstants.
        /// We can't access those values from our assembly so we will copy those values.
        /// Once a BlockchainConstants value changes, this test will fail. That means you will
        /// need to check all custom parameters in other projects which defer from the usual consensus rules!
        /// </summary>
        [TestMethod]
        public void GetPreviousDifficultyUpdateInformation_Uses_ConstantValues()
        {
            var difficultyUpdateCycle = 10;
            var blockchainHeight = -1;
            var selfCallingMock = new Mock<DifficultyCalculator>(MockBehavior.Strict);
            var blockchainMock = new Mock<Blockchain>(MockBehavior.Strict, new object[] { _netId });
            blockchainMock.Setup(m => m.CurrentHeight).Returns(blockchainHeight);
            var blockchain = blockchainMock.Object;
            BlockDifficultyUpdate expectedInformation = new BlockDifficultyUpdate(blockchain, -1, -1);
            selfCallingMock.Setup(m => m.GetPreviousDifficultyUpdateInformation(blockchain)).CallBase();
            selfCallingMock.Setup(m => m.GetPreviousDifficultyUpdateInformation(blockchainHeight, blockchain, difficultyUpdateCycle)).Returns(expectedInformation);

            var result = selfCallingMock.Object.GetPreviousDifficultyUpdateInformation(blockchain);

            Assert.AreEqual(expectedInformation, result);
            selfCallingMock.VerifyAll();
            blockchainMock.VerifyAll();
        }

        /// <summary>
        /// The second overload of the GetPreviousDifficultyUpdateInformation method uses 
        /// Blockchain.CurrentHeight to call the full GetPreviousDifficultyUpdateInformation method.
        /// This test ensures that there are no modifications done by this 'hatch' method.
        /// </summary>
        [TestMethod]
        public void GetPreviousDifficultyUpdateInformation_Calls_BlockchainCurrentHeight()
        {
            var difficultyUpdateCycle = 10;
            var blockchainHeight = -1;
            var selfCallingMock = new Mock<DifficultyCalculator>(MockBehavior.Strict);
            var blockchainMock = new Mock<Blockchain>(MockBehavior.Strict, new object[] { _netId });
            blockchainMock.Setup(m => m.CurrentHeight).Returns(blockchainHeight);
            var blockchain = blockchainMock.Object;
            BlockDifficultyUpdate expectedInformation = new BlockDifficultyUpdate(blockchain, -1, -1);
            selfCallingMock.Setup(m => m.GetPreviousDifficultyUpdateInformation(blockchain, difficultyUpdateCycle)).CallBase();
            selfCallingMock.Setup(m => m.GetPreviousDifficultyUpdateInformation(blockchainHeight, blockchain, difficultyUpdateCycle)).Returns(expectedInformation);

            var result = selfCallingMock.Object.GetPreviousDifficultyUpdateInformation(blockchain, difficultyUpdateCycle);

            Assert.AreEqual(expectedInformation, result);
            selfCallingMock.VerifyAll();
            blockchainMock.VerifyAll();
        }
        
        [TestMethod]
        public void GetPreviousDifficultyUpdateInformationFull_ForHeight9_ThrowException()
        {
            var difficultyUpdateCycle = 10;
            var blockchainHeight = 9;
            var sut = new DifficultyCalculator();
            var blockchain = new Blockchain(_netId);

            var ex = Assert.ThrowsException<DifficultyCalculationException>(
                    () => sut.GetPreviousDifficultyUpdateInformation(blockchainHeight, blockchain, difficultyUpdateCycle)
                );

            Assert.AreEqual("Unable to calculate the previous difficulty because the height is lower than the DifficultyUpdateCycle.", ex.Message);
        }

        [TestMethod]
        public void GetPreviousDifficultyUpdateInformationFull_ForHeight10_ReturnsCorrectInformation()
        {
            var difficultyUpdateCycle = 10;
            var blockchainHeight = 10; // = index and that starts at zero.
            var sut = new DifficultyCalculator();
            var blocks = new List<Block>();
            // Add 11 blocks
            for (int i = 10; i < 111; i = i+10)
            {
                blocks.Add(new Block(_netId, _protocol, "abc", i, new List<AbstractTransaction>()));
            }
            var blockchain = new Blockchain(blocks, _netId);

            BlockDifficultyUpdate result = sut.GetPreviousDifficultyUpdateInformation(blockchainHeight, blockchain, difficultyUpdateCycle);

            Assert.AreEqual(blockchain, result.Blockchain);
            Assert.AreEqual(0, result.BeginHeight);
            Assert.AreEqual(9, result.EndHeight);
            Assert.AreEqual(90, result.TotalSecondsForBlocks); // It took 90 seconds to create 9 blocks
        }

        [TestMethod]
        public void GetPreviousDifficultyUpdateInformationFull_ForHeight62_ReturnsCorrectInformation()
        {
            var difficultyUpdateCycle = 10;
            var blockchainHeight = 63; // = index and that starts at zero.
            var sut = new DifficultyCalculator();
            var blocks = new List<Block>();
            // Add 64 blocks
            for (int i = 10; i < 641; i = i + 10)
            {
                blocks.Add(new Block(_netId, _protocol, "abc", i, new List<AbstractTransaction>()));
            }
            var blockchain = new Blockchain(blocks, _netId);

            BlockDifficultyUpdate result = sut.GetPreviousDifficultyUpdateInformation(blockchainHeight, blockchain, difficultyUpdateCycle);

            Assert.AreEqual(blockchain, result.Blockchain);
            Assert.AreEqual(50, result.BeginHeight);
            Assert.AreEqual(59, result.EndHeight);
            Assert.AreEqual(90, result.TotalSecondsForBlocks); // It took 90 seconds to create 9 blocks
        }
    }
}
