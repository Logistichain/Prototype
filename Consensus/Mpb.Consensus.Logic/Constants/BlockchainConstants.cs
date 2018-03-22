using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Mpb.Consensus.Logic.Constants
{
    internal class BlockchainConstants
    {
        internal const int BlockVersion = 1;
        internal const int TransactionVersion = 1;
        internal const int ProtocolVersion = 1;
        internal const int SecondsPerBlockGoal = 15; // We want to create one block each 15 seconds (average)
        internal const int DifficultyUpdateCycle = 5; // Every 5 blocks, the difficulty will be readjusted
        internal static readonly BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
    }
}
