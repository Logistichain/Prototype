using System;

namespace Mpb.Consensus.Logic.MiscLogic
{
    public interface ITimestamper
    {
        long GetCurrentUtcTimestamp();

        long GetUtcTimestamp(DateTime dateTime);
    }
}