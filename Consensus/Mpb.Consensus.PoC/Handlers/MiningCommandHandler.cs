using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.PoC.Handlers
{
    internal class MiningCommandHandler
    {
        private readonly Miner _miner;

        internal MiningCommandHandler(Miner miner)
        {
            _miner = miner;
        }

        internal void HandleStartMiningCommand()
        {
            _miner.StartMining();
            Console.Write("> ");
        }

        internal void HandleStopMiningCommand()
        {
            _miner.StopMining(true);
        }
    }
}
