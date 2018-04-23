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
        /// Validates a block. Throws BlockRejectedException if the validation fails.
        /// </summary>
        /// <param name="block">The block to validate</param>
        /// <param name="currentTarget">The current target to validate the block's hash</param>
        /// <param name="blockchain">The current blockchain to check for doublespending</param>
        /// <param name="writeToBlockchain">Append the given block to the given blockchain, this also covers the scenario where multiple blocks arrive at the same time</param>
        void ValidateBlock(Block block, BigDecimal currentTarget, Blockchain blockchain, bool writeToBlockchain);
    }
}