using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.MiscLogic;

namespace Mpb.Consensus.Test
{
    /// <summary>
    /// This class tests some of the unix timestamper functionalities, but not all
    /// because the timestamper uses local system time on unspecified datetime objects.
    /// So system time tests may fail on other computers from different timezones.
    /// </summary>
    [TestClass]
    public class UnixTimestamperTest
    {
        [TestMethod]
        public void GetCurrentUtcTimestamp_Calls_GetUtcTimestamp()
        {
            var mock = new Mock<UnixTimestamper>() { CallBase = true };
            var expectedResult = 123L;
            mock.Setup((m) => m.GetUtcTimestamp(It.IsAny<DateTime>())).Returns(expectedResult);
            var sut = mock.Object;

            var result = sut.GetCurrentUtcTimestamp();

            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void GetUtcTimestamp_Handles_SmallValue()
        {
            var sut = new UnixTimestamper();
            var smallDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = sut.GetUtcTimestamp(smallDateTime);

            Assert.AreEqual(result, -62135596800);
        }

        [TestMethod]
        public void GetUtcTimestamp_Returns_ZeroOnOrigin()
        {
            var sut = new UnixTimestamper();
            var timestampOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = sut.GetUtcTimestamp(timestampOrigin);

            Assert.AreEqual(result, 0);
        }
    }
}
