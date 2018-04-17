﻿using Mpb.Model;
using Mpb.Shared;

namespace Mpb.Consensus.BlockLogic
{
    /// <summary>
    /// Validate blocks before accepting them.
    /// </summary>
    public interface IBlockValidator
    {
        /// <summary>
        /// Validates a block. Throws BlockRejectedException if the validation fails.
        /// </summary>
        /// <param name="block">The block to validate</param>
        /// <param name="currentTarget">The current target to validate the block's hash</param>
        /// <param name="setBlockHash">Whether this method needs to calculate and add the hash to the block</param>
        void ValidateBlock(Block block, BigDecimal currentTarget, bool setBlockHash);
    }
}