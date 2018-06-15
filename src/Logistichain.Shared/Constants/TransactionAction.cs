using System;

namespace Logistichain.Shared.Constants
{
    /// <summary>
    /// Max length = 16 chars.
    /// Increase the limit in the Networking module
    /// <seealso cref="Logistichain.Networking.Model.MessagePayloads.StateBlockPayload"/>
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
