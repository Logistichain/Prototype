using System;

namespace Mpb.Shared.Constants
{
    public enum TransactionAction
    {
        TransferToken,
        TransferSupply,
        DestroySupply,
        CreateSupply,
        CreateSku,
        ChangeSku,
        ClaimCoinbase
    }
}
