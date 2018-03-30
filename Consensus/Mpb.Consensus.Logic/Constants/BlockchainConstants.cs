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
        internal const string DefaultNetworkIdentifier = "testnet";
        internal const uint TransactionVersion = 1;
        internal const uint ProtocolVersion = 1;
        internal const uint SecondsPerBlockGoal = 15; // We want to create one block each 15 seconds (average)
        internal const int DifficultyUpdateCycle = 10; // Every x blocks, the difficulty will be readjusted
        internal static readonly BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);

        // Transaction fees (expressed in tokens, not SKU's or supply!)
        internal const uint TransferTokenFee = 10;
        internal const uint TransferSupplyFee = 1;
        internal const uint DestroySupplyFee = 1;
        internal const uint CreateSkuFee = 100;
        internal const uint ChangeSkuFee = 100;
        internal const uint CreateSupplyFee = 100;
        internal const uint CoinbaseReward = 5000;
    }
}
