using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.MiscLogic;
using System.Numerics;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;

namespace Mpb.Consensus.Test
{
    [TestClass]
    public class PowBlockCreatorTest
    {
        [TestMethod]
        public async Task CreateValidBlock()
        {
            var timestamper = new UnixTimestamper();
            var sut = new PowBlockCreator(timestamper);
            byte[] b = new byte[256 / 8 + 1];
            Array.Fill(b, (byte)0xFF);
            b[0] = 0; // First 8 bits are zeroes so value is positive
            BigInteger biggestTarget = new BigInteger(b);

            var result = await sut.CreateValidBlock(biggestTarget);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DifficultyTest()
        {
            var sha256 = SHA256.Create();
            var blockHash = sha256.ComputeHash(Encoding.BigEndianUnicode.GetBytes("Hello World!"));
            BigInteger difficulty1 = new BigInteger(blockHash);

            var blockHash2 = sha256.ComputeHash(Encoding.BigEndianUnicode.GetBytes("Hello World!!"));
            BigInteger difficulty2 = new BigInteger(blockHash2);
        }
    }
}
