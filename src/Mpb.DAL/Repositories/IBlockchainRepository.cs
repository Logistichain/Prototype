using Mpb.Model;

namespace Mpb.DAL
{
    public interface IBlockchainRepository
    {
        void Delete(string netIdentifier);
        Blockchain GetChainByNetId(string netIdentifier);
        Block GetBlockByHash(string hash, string netIdentifier);
        Block GetBlockByTransactionHash(string transactionHash, string netIdentifier);
        int GetHeightForBlock(string hash, string netIdentifier);
        void Update(Blockchain chain);
        Block GetBlockByPreviousHash(string hash, string netIdentifier);
    }
}