using Logistichain.Networking.Constants;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Logistichain.Networking.Events
{
    public class SyncStatusChangedEventArgs : EventArgs
    {
        public SyncStatusChangedEventArgs(SyncStatus oldStatus, SyncStatus status)
        {
            OldStatus = oldStatus;
            NewStatus = status;
        }

        public SyncStatus OldStatus { get; }
        public SyncStatus NewStatus { get; }
    }
}
