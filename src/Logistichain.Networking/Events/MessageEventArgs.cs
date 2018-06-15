using Logistichain.Networking.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Logistichain.Networking.Events
{
    internal class MessageEventArgs : EventArgs
    {
        internal MessageEventArgs(Message msg)
        {
            Message = msg;
        }

        internal Message Message { get; }
    }
}
