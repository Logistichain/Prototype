using Mpb.Networking.Model;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mpb.Networking
{
    public interface INetworkManager
    {
        /// <summary>
        /// The port that is currently open for listening
        /// </summary>
        ushort ListeningPort { get; }

        /// <summary>
        /// Our public IP address so we can publish our address+port to other nodes
        /// </summary>
        IPAddress PublicIp { get; }

        bool IsDisposed { get; }

        /// <summary>
        /// When this is true, we are not processing new blocks and transactions from other nodes
        /// </summary>
        bool IsSyncing { get; set; }

        /// <summary>
        /// Start listening to incoming TCP sockets
        /// </summary>
        /// <returns>void</returns>
        Task AcceptConnections(IPAddress publicIp, ushort listenPort, CancellationTokenSource cts);

        /// <summary>
        /// Connect to an outgoing network node
        /// </summary>
        /// <param name="node">The peer to connect to</param>
        Task ConnectToPeer(NetworkNode node);

        /// <summary>
        /// Connect to an endpoint
        /// </summary>
        /// <param name="endpoint">The peer to connect to</param>
        Task ConnectToPeer(IPEndPoint endpoint);

        void DisconnectPeer(IPEndPoint iPEndPoint);

        void Dispose();
    }
}