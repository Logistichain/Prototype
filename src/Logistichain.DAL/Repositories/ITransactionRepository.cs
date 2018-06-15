using System.Collections.Generic;
using Logistichain.Model;

namespace Logistichain.DAL
{
    public interface ITransactionRepository
    {
        ulong GetTokenBalanceForPubKey(string pubKey, string netId);
        IEnumerable<AbstractTransaction> GetAll(string netId);
        IEnumerable<AbstractTransaction> GetAllByPublicKey(string pubKey, string netId);
        IEnumerable<AbstractTransaction> GetAllReceivedByPublicKey(string pubKey, string netId);
        IEnumerable<AbstractTransaction> GetAllSentByPublicKey(string pubKey, string netId);
    }
}