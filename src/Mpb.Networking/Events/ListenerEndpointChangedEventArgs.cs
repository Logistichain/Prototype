using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Mpb.Networking.Events
{
    public class ListenerEndpointChangedEventArgs : EventArgs
    {
        public ListenerEndpointChangedEventArgs(IPEndPoint oldEndpoint, IPEndPoint newEndpoint)
        {
            OldEndpoint = oldEndpoint;
            NewEndpoint = newEndpoint;
        }

        public IPEndPoint OldEndpoint { get; }
        public IPEndPoint NewEndpoint { get; }
    }
}
