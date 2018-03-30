using System.Collections.Generic;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.DAL
{
    // Todo update documentation
    public interface ITransactionRepository
    {
        IEnumerable<AbstractTransaction> GetAll(string netId);
        IEnumerable<AbstractTransaction> GetAllByPublicKey(string pubKey, string netId);
        IEnumerable<AbstractTransaction> GetAllReceivedByPublicKey(string pubKey, string netId);
        IEnumerable<AbstractTransaction> GetAllSentByPublicKey(string pubKey, string netId);
    }
}