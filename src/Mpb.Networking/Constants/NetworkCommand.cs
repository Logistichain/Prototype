using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Networking.Constants
{
    public enum NetworkCommand
    {
        Version,
        VerAck,
        Addr,
        GetAddr,
        CloseConn,
        GetHeaders,
        Headers
    }
}
