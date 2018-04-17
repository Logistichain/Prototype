using System.Collections.Generic;
using Mpb.Model;

namespace Mpb.DAL
{
    public interface ISkuRepository
    {
        IEnumerable<IEnumerable<Sku>> GetAllWithHistory(string netId);
        IEnumerable<Sku> GetSkuWithHistory(string createdSkuBlockHash, int skuTxIndex, string netId);
        ulong GetSupplyBalanceForPubKey(string publicKey, IEnumerable<Sku> skuHistory);
        ulong GetSupplyBalanceForPubKey(string publicKey, string createdSkuBlockHash, int skuTxIndex, string netId);
    }
}