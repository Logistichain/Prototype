using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Networking.Constants
{
    public enum NetworkCommand
    {
        Version,
        VerAck,
        Addr,
        GetAddr,
        CloseConn,
        GetHeaders,
        Headers,
        GetBlocks,
        Blocks,
        NotFound,
        NewBlock,
        NewTransaction,
        TxPool,
        GetTxPool
    }
}
