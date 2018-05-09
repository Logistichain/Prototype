using System;

namespace Mpb.Shared.Constants
{
    /// <summary>
    /// Max length = 16 chars.
    /// Increase the limit in the Networking module
    /// <seealso cref="Mpb.Networking.Model.MessagePayloads.StateBlockPayload"/>
    /// </summary>
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
