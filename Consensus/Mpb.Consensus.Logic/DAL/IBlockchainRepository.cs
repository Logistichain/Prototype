using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.DAL
{
    // Todo update documentation
    public interface IBlockchainRepository
    {
        void Delete(string netIdentifier);
        Blockchain GetByNetId(string netIdentifier);
        void Update(Blockchain chain);
    }
}