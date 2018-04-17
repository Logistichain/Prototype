using System;
using System.Globalization;
using System.Numerics;

namespace Mpb.Shared.Constants
{
    public class BlockchainConstants
    {
        public const string DefaultNetworkIdentifier = "testnet";
        public const uint TransactionVersion = 1;
        public const uint ProtocolVersion = 1;
        public const uint SecondsPerBlockGoal = 15; // We want to create one block each 15 seconds (average)
        public const int DifficultyUpdateCycle = 10; // Every x blocks, the difficulty will be readjusted
        public static readonly BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);

        // Transaction fees (expressed in tokens, not SKU's or supply!)
        public const uint TransferTokenFee = 10;
        public const uint TransferSupplyFee = 1;
        public const uint DestroySupplyFee = 1;
        public const uint CreateSkuFee = 100;
        public const uint ChangeSkuFee = 100;
        public const uint CreateSupplyFee = 100;
        public const uint CoinbaseReward = 5000;
    }
}
