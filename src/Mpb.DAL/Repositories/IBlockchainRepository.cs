using Mpb.Model;

namespace Mpb.DAL
{
    // Todo update documentation
    public interface IBlockchainRepository
    {
        void Delete(string netIdentifier);
        Blockchain GetChainByNetId(string netIdentifier);
        Block GetBlockByHash(string blockHash, string netIdentifier);
        Block GetBlockByTransactionHash(string transactionHash, string netIdentifier);
        int GetHeightForBlock(string hash, string netIdentifier);
        void Update(Blockchain chain);
    }
}