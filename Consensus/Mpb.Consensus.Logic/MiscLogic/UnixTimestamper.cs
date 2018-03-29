using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Logic.MiscLogic
{
    public class UnixTimestamper : ITimestamper
    {
        /// <summary>
        /// Gets the current UTC date and time and converts it to a unix timestamp (accuracy = seconds)
        /// </summary>
        /// <returns>Unix timestamp (UTC timezone)</returns>
        public virtual long GetCurrentUtcTimestamp()
        {
            return GetUtcTimestamp(DateTime.UtcNow);
        }

        /// <summary>
        /// Transforms the given dateTime to UTC and converts it to a unix timestamp (accuracy = seconds)
        /// </summary>
        /// <param name="dateTime">Datetime (any timezone)</param>
        /// <returns>Unix timestamp (UTC timezone)</returns>
        public virtual long GetUtcTimestamp(DateTime dateTime)
        {
            DateTime unixStartDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime utcDateTime = dateTime.ToUniversalTime();

            return (long)(utcDateTime - unixStartDateTime).TotalSeconds;
        }

        public virtual DateTime GetUtcDateTimeFromTimestamp(long timestamp)
        {
            DateTime unixStartDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return unixStartDateTime.AddSeconds(timestamp);
        }
    }
}
