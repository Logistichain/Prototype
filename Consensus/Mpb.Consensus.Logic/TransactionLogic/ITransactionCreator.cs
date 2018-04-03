using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.TransactionLogic
{
    // Todo update documentation
    public interface ITransactionCreator
    {
        AbstractTransaction CreateCoinBaseTransaction(string creatorPubKey, string creatorPrivKey, string optionalData);
        AbstractTransaction CreateSkuCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, SkuData sku);
        AbstractTransaction CreateSupplyCreationTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);
        AbstractTransaction CreateSupplyDestroyTransaction(string ownerPubKey, string ownerPrivKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);
        AbstractTransaction CreateSupplyTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string skuBlockHash, int skuTxIndex, string optionalData);
        AbstractTransaction CreateTokenTransferTransaction(string fromPubKey, string fromPrivKey, string toPubKey, uint amount, string optionalData);
    }
}