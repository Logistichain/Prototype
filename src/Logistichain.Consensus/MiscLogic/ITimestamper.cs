using System;

namespace Logistichain.Consensus.MiscLogic
{
    /// <summary>
    /// Timestamp blocks and transactions using this implementation.
    /// A timestamp is a UTC DateTime, converted to a second-based unix epoch.
    /// </summary>
    public interface ITimestamper
    {
        /// <summary>
        /// Gets the current UTC unix timestamp.
        /// </summary>
        /// <returns>Second-based unix timestamp</returns>
        long GetCurrentUtcTimestamp();

        /// <summary>
        /// Calculates the UTC unix timestamp from the given datetime.
        /// </summary>
        /// <param name="dateTime">The datetime to calculate the timestamp of</param>
        /// <returns>Second-based unix timestamp</returns>
        long GetUtcTimestamp(DateTime dateTime);

        /// <summary>
        /// Calculates the DateTime from the given unix timestamp.
        /// </summary>
        /// <param name="timestamp">The second-based unix timestamp</param>
        /// <returns>UTC datetime object</returns>
        DateTime GetUtcDateTimeFromTimestamp(long timestamp);
    }
}