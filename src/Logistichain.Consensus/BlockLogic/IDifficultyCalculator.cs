using Logistichain.Model;
using Logistichain.Shared;

namespace Logistichain.Consensus.BlockLogic
{
    /// <summary>
    /// Calculate mining difficulty.
    /// </summary>
    public interface IDifficultyCalculator
    {
        /// <summary>
        /// Calculates the difficulty for the current block height, following the consensus rules.
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <returns>The current difficulty</returns>
        BigDecimal CalculateCurrentDifficulty(Blockchain chain);

        /// <summary>
        /// Calculate difficulty for a specific block height.
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <param name="height">The height of the block in the blockchain</param>
        /// <returns>The difficulty, active for that block height</returns>
        BigDecimal CalculateDifficultyForHeight(Blockchain chain, int height);

        /// <summary>
        /// Calculate the difficulty with custom parameters.
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <param name="height">The height of the block in the blockchain</param>
        /// <param name="protocolVersion">The protocol version to specify in which way the difficulty will be calculated</param>
        /// <param name="secondsPerBlockGoal">The desired amount of seconds needed to create a block</param>
        /// <param name="difficultyUpdateCycle">The cycle (x blocks) in which the difficulty is readjusted</param>
        /// <returns>The difficulty value for the given scenario</returns>
        BigDecimal CalculateDifficulty(Blockchain chain, int height, uint protocolVersion, uint secondsPerBlockGoal, int difficultyUpdateCycle);

        /// <summary>
        /// Get information about the previous difficulty cycle, following consensus rules.
        /// Example: If the consensus update cycle is 10 and the current height is 78,
        /// this method will return information about the difficulty for the blocks 60-70.
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <returns>Information about the last block difficulty update</returns>
        BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain);

        /// <summary>
        /// Get information about the previous difficulty cycle with custom update cycle.
        /// Example: If the update cycle is 10 and the current height is 46,
        /// this method will return information about the difficulty for the blocks 30-40.
        /// </summary>
        /// <param name="chain">The blockchain to calculate the difficulty from</param>
        /// <param name="difficultyUpdateCycle">The cycle (x blocks) in which the difficulty is readjusted</param>
        /// <returns>Information about the last block difficulty update</returns>
        BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(Blockchain chain, int difficultyUpdateCycle);

        /// <summary>
        /// Get information about the previous difficulty cycle with custom parameters.
        /// Example: If the update cycle is 10 and the given height is 32, 
        /// this method will return information about the difficulty for the blocks 20-30.
        /// </summary>
        /// <param name="height">The height of the block in the blockchain</param>
        /// <param name="chain">The blockchain to get the difficulty info from</param>
        /// <param name="difficultyUpdateCycle">The cycle (x blocks) in which the difficulty is readjusted</param>
        /// <returns>Information about the last block difficulty update</returns>
        BlockDifficultyUpdate GetPreviousDifficultyUpdateInformation(int height, Blockchain chain, int difficultyUpdateCycle);
    }
}