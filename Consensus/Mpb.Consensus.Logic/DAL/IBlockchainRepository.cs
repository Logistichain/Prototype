using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.DAL
{
    public interface IBlockchainRepository
    {
        Blockchain GetByNetId(string netIdentifier);
        void Update(Blockchain chain);
    }
}