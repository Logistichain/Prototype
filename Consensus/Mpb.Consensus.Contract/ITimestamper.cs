using System;

namespace Mpb.Consensus.Contract
{
    public interface ITimestamper
    {
        long GetCurrentUtcTimestamp();

        long GetUtcTimestamp(DateTime dateTime);
    }
}