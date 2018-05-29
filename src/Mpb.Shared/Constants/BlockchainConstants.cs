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

        /// <summary>
        /// The current transaction version
        /// </summary>
        public const uint TransactionVersion = 1;

        /// <summary>
        /// The current consensus protocol version
        /// A different protocol version means different block/cryptography validations are applied
        /// </summary>
        public const uint ProtocolVersion = 1;

        /// <summary>
        /// We want to create one block each 15 seconds (average)
        /// </summary>
        public const uint SecondsPerBlockGoal = 15;

        /// <summary>
        /// Every x blocks, the difficulty will be readjusted
        /// </summary>
        public const int DifficultyUpdateCycle = 10;

        /// <summary>
        /// The block's timestamp versus current UTC time cannot be higher than +120 or lower than -120 (in seconds)
        /// </summary>
        public const int MaximumTimestampOffset = 120;

        /// <summary>
        /// The easiest possible target for mining, used when mining the first few blocks
        /// </summary>
        public static readonly BigDecimal MaximumTarget = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber);

        /// <summary>
        /// Maximum allowed transactions per block
        /// </summary>
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
