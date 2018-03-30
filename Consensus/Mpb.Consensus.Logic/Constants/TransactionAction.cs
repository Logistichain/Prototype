using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Mpb.Consensus.Logic.Constants
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
