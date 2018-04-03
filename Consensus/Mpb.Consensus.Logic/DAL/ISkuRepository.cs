using System.Collections.Generic;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.DAL
{
    public interface ISkuRepository
    {
        IEnumerable<IEnumerable<Sku>> GetAllWithHistory(string netId);
        IEnumerable<Sku> GetSkuWithHistory(string createdSkuBlockHash, int skuTxIndex, string netId);
        long GetSupplyBalanceForPubKey(string publicKey, IEnumerable<Sku> skuHistory);
        long GetSupplyBalanceForPubKey(string publicKey, string createdSkuBlockHash, int skuTxIndex, string netId);
    }
}