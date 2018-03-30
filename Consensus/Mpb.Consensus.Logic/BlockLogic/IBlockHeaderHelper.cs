using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.BlockLogic
{
    // Todo update documentation
    public interface IBlockHeaderHelper
    {
        byte[] GetBlockHeaderBytes(Block block);
    }
}