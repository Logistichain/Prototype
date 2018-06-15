using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Networking.Events
{
    // Internal networking events, that's why these aren't located in the Shared assembly.

    internal delegate void MessageReceivedEventHandler(object sender, MessageEventArgs eventHandler);
    public delegate void DisconnectedEventHandler(object sender);
    internal delegate void ListenerEndpointChangedEventHandler(object sender, ListenerEndpointChangedEventArgs eventHandler);
    internal delegate void SyncStatusChangedEventHandler(object sender, SyncStatusChangedEventArgs eventHandler);
}
