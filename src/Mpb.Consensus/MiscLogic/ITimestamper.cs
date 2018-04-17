using System;

namespace Mpb.Consensus.MiscLogic
{
    // Todo update documentation
    public interface ITimestamper
    {
        long GetCurrentUtcTimestamp();

        long GetUtcTimestamp(DateTime dateTime);
        DateTime GetUtcDateTimeFromTimestamp(long timestamp);
    }
}