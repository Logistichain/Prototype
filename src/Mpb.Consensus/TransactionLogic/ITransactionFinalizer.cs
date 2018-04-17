using Mpb.Model;

namespace Mpb.Consensus.BlockLogic
{
    public interface ITransactionFinalizer
    {
        string CalculateHash(AbstractTransaction transaction);
        string CreateSignature(AbstractTransaction transaction);
        void FinalizeTransaction(AbstractTransaction tx, string fromPubKey, string fromPrivKey);
        byte[] GetTransactionBytes(AbstractTransaction transaction);
    }
}