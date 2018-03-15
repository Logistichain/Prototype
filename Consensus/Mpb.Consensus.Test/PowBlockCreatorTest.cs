using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Logic.MiscLogic;
using System.Numerics;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using Mpb.Consensus.Model;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Globalization;

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
            var maximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", System.Globalization.NumberStyles.HexNumber);
            var blocksList = new List<Block>();
            var timestampList = new List<DateTime>();
            var targetList = new List<BigInteger>();
            Block lastBlock = null;
            int i = 1;
            while(blocksList.Count < 150)
            {
                if (i % 11 == 0)
                {
                    // Every 10 times, recalculate difficulty
                    lock(timestampList)
                    {
                        var previousDateTime = timestampList[timestampList.Count - 10];
                        var timeForLastTenBlocks = DateTime.UtcNow - previousDateTime;
                        if (timeForLastTenBlocks.TotalSeconds > 0)
                        {
                            var targetChangePercentage = (BigInteger)(150 / timeForLastTenBlocks.TotalSeconds);
                            maximumTarget = targetChangePercentage == 0 ? maximumTarget : (maximumTarget / targetChangePercentage);
                        }
                    }
                }
                lock (timestampList)
                {
                    timestampList.Add(DateTime.UtcNow);
                    targetList.Add(maximumTarget);
                    lastBlock = sut.CreateValidBlock(maximumTarget).Result;
                    blocksList.Add(lastBlock);
                }
                i++;
            }

            // Logging
            string s = "Gestart || Gestopt ||| Seconden |||| Hash ||||| Difficulty |||||| Target\n";
            var sha256 = SHA256.Create();
            for (int timestampIndex = 0; timestampIndex < timestampList.Count; timestampIndex++)
            {
                var target = targetList[timestampIndex];
                var startedTimestamp = timestampList[timestampIndex];
                var block = blocksList[timestampIndex];
                var finishedTimestamp = timestamper.GetUtcDateTimeFromTimestamp(block.Timestamp);
                var timeToCreate = block.Timestamp - timestamper.GetUtcTimestamp(startedTimestamp);

                var blockHash = sha256.ComputeHash(sut.GetBlockHeaderBytes(block));
                var hashString = BitConverter.ToString(blockHash).Replace("-", "");
                var hashValue = BigInteger.Parse(hashString, NumberStyles.HexNumber);

                s += startedTimestamp.ToString("HH:mm:ss") + " || " + finishedTimestamp.ToString("HH:mm:ss") + " ||| " + timeToCreate + " |||| " + hashString + " ||||| " + hashValue + " ||||| " + target + "\n";
            }

            File.WriteAllText(@"C:\Users\Gebruiker\Documents\repo\stockchain\Consensus\Mpb.Consensus.Logic\mining_log.txt", s);
        }
    }
}
