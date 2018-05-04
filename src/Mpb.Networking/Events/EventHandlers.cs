using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Networking.Events
{
    internal delegate void MessageReceivedEventHandler(object sender, MessageEventArgs eventHandler);
    public delegate void DisconnectedEventHandler(object sender);
    internal delegate void ListenerEndpointChangedEventHandler(object sender, ListenerEndpointChangedEventArgs eventHandler);
}
