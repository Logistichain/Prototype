using Mpb.Model;

namespace Mpb.Consensus.BlockLogic
{
    // Todo update documentation
    public interface IBlockHeaderHelper
    {
        byte[] GetBlockHeaderBytes(Block block);
    }
}