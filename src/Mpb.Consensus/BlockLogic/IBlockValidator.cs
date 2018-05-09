using Mpb.Model;
using Mpb.Shared;

namespace Mpb.Consensus.BlockLogic
{
    /// <summary>
    /// Validate blocks before accepting/relaying them.
    /// </summary>
    public interface IBlockValidator
    {
        /// <summary>
        /// Validates a complete block. Throws BlockRejectedException if the validation fails.
        /// </summary>
        /// <param name="block">The block to validate</param>
        /// <param name="currentTarget">The current target to validate the block's hash</param>
        /// <param name="blockchain">The current blockchain to check for doublespending</param>
        /// <param name="checkTimestamp">Whether to check if the timestamp is within the acceptable range compared to the current UTC system time. Set this to false when syncing the blockchain from other nodes</param>
        /// <param name="writeToBlockchain">Append the given block to the given blockchain, this also covers the scenario where multiple blocks arrive at the same time</param>
        void ValidateBlock(Block block, BigDecimal currentTarget, Blockchain blockchain, bool checkTimestamp, bool writeToBlockchain);

        /// <summary>
        /// Validate the block header only. Throws BlockRejectedException if the validation fails.
        /// </summary>
        /// <param name="block">The block to validate</param>
        /// <param name="hashValue">The pre-calculated hash value of the block</param>
        /// <param name="currentTarget">The current target to validate the block's hash</param>
        /// <param name="checkTimestamp">Whether to check if the timestamp is within the acceptable range compared to the current UTC system time. Set this to false when syncing the blockchain from other nodes</param>
        void ValidateBlockHeader(Block block, BigDecimal hashValue, BigDecimal currentTarget, bool checkTimestamp);
    }
}