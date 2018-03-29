using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public interface IBlockHeaderHelper
    {
        byte[] GetBlockHeaderBytes(Block block);
    }
}