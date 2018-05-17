using System;
using System.Globalization;
using System.Numerics;

namespace Mpb.Shared.Constants
{
    public class BlockchainConstants
    {
        /// <summary>
        /// Max 7 characters.
        /// <seealso cref="Mpb.Networking.Model.MessagePayloads.HeadersPayload"/>
        /// </summary>
        public const string DefaultNetworkIdentifier = "testnet";
        public const uint TransactionVersion = 1;
        public const uint ProtocolVersion = 1;
        public const uint SecondsPerBlockGoal = 15; // We want to create one block each 15 seconds (average)
        public const int DifficultyUpdateCycle = 10; // Every x blocks, the difficulty will be readjusted
        public const int MaximumTimestampOffset = 120; // The block's timestamp versus current UTC time cannot be higher than +120 or lower than -120 (in seconds)
        public static readonly BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);
        public const int MaximumTransactionPerBlock = 1000;

        // Transaction fees (expressed in tokens, not SKU's or supply!)
        /*
        public const uint TransferTokenFee = 10;
        public const uint TransferSupplyFee = 1;
        public const uint DestroySupplyFee = 1;
        public const uint CreateSkuFee = 100;
        public const uint ChangeSkuFee = 100;
        public const uint CreateSupplyFee = 100;
        public const uint CoinbaseReward = 5000;
        */

        public const uint TransferTokenFee = 0;
        public const uint TransferSupplyFee = 0;
        public const uint DestroySupplyFee = 0;
        public const uint CreateSkuFee = 0;
        public const uint ChangeSkuFee = 0;
        public const uint CreateSupplyFee = 0;
        public const uint CoinbaseReward = 5000;
    }
}
