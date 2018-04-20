using Mpb.Model;

namespace Mpb.Consensus.BlockLogic
{
    // Todo update documentation
    public interface IBlockFinalizer
    {
        byte[] GetBlockHeaderBytes(Block block);
        void FinalizeBlock(Block block, string validHash, string privKey);
        string CreateSignature(Block block, string privKey);
        string CalculateHash(Block block);
    }
}