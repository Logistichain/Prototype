using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Networking.Constants
{
    /// <summary>
    /// The various steps of synchronizing/downloading the blockchain from another node.
    /// </summary>
    public enum SyncStatus
    {
        NotSyncing,
        Initiated,
        InProgress,
        Failed,
        Succeeded
    }
}
