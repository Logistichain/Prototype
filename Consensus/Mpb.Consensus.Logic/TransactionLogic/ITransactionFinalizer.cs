using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public interface ITransactionFinalizer
    {
        string CalculateHash(AbstractTransaction transaction);
        string CalculateSignature(AbstractTransaction transaction);
        void FinalizeTransaction(AbstractTransaction tx, string fromPubKey, string fromPrivKey);
        byte[] GetTransactionBytes(AbstractTransaction transaction);
    }
}