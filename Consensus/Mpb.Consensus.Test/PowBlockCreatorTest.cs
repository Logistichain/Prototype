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
        public void CreateValidBlockSync()
        {
            var timestamper = new UnixTimestamper();
            var sut = new PowBlockCreator(timestamper);
            var blocksList = new List<Block>();
            var blockCreatedTimestampList = new List<DateTime>();
            var difficultyList = new List<BigDecimal>();
            BigDecimal maximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
            BigDecimal currentDifficulty = 1;
            Block lastBlock = null;
            int i = 1;
            while (blocksList.Count < 99)
            {
                if (i % 10 == 0)
                {
                    // Every 10 times, recalculate difficulty
                    var previousDateTime = blockCreatedTimestampList[blockCreatedTimestampList.Count - 9];
                    var timeForLastTenBlocks = DateTime.UtcNow - previousDateTime;
                    if (timeForLastTenBlocks.TotalSeconds > 0)
                    {
                        var difficultyAdjustmentPercentage = (20 / timeForLastTenBlocks.TotalSeconds);
                        currentDifficulty = currentDifficulty * difficultyAdjustmentPercentage;
                    }
                }
                var target = maximumTarget / currentDifficulty;
                difficultyList.Add(target);
                lastBlock = sut.CreateValidBlock(target);
                blockCreatedTimestampList.Add(DateTime.UtcNow);
                blocksList.Add(lastBlock);

                i++;
            }

            // Logging
            string s = "Gestart || Gestopt ||| Seconden |||| Hash ||||| Target\n";
            var sha256 = SHA256.Create();
            var lastTenBlocksTotalTime = new TimeSpan();
            for (int timestampIndex = 0; timestampIndex < blockCreatedTimestampList.Count; timestampIndex++)
            {
                var target = difficultyList[timestampIndex];
                var block = blocksList[timestampIndex];
                var startedTimestamp = timestamper.GetUtcDateTimeFromTimestamp(block.Timestamp);
                var finishedTimestamp = blockCreatedTimestampList[timestampIndex];
                var timeToCreate = finishedTimestamp - startedTimestamp;
                lastTenBlocksTotalTime += timeToCreate;

                var blockHash = sha256.ComputeHash(sut.GetBlockHeaderBytes(block));
                var hashString = BitConverter.ToString(blockHash).Replace("-", "");
                var hashValue = BigInteger.Parse(hashString, NumberStyles.HexNumber);

                s += startedTimestamp.ToString("HH:mm:ss") + " || " + finishedTimestamp.ToString("HH:mm:ss") + " ||| " + timeToCreate + " |||| " + hashString + " ||||| " + target + "\n";
            }

            File.WriteAllText(@"C:\Users\Gebruiker\Documents\repo\stockchain\Consensus\Mpb.Consensus.Logic\mining_log.txt", s);
        }
    }
}
