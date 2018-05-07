using Microsoft.Extensions.Logging;
using Mpb.DAL;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;
using Mpb.Networking.Model.MessagePayloads;
using Mpb.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Inspired by NEO, modified by Montapacking
namespace Mpb.Networking
{
    public class NetworkManager : IDisposable, INetworkManager
    {
        private TcpListener _listener;
        private NetworkNodesPool _nodePool;
        private ushort _port;
        private IPAddress _publicIp;
        private bool _isDisposed = false;
        private bool _isSyncing = true; // When this is true, we are not processing new blocks and transactions from other nodes
        private ILogger _logger;
        private IMessageHandler _messageHandler;
        private IMessageHandler _handshakeMessageHandler;

        public NetworkManager(NetworkNodesPool nodePool, ILoggerFactory loggerFactory, IBlockchainRepository repo)
        {
            _logger = loggerFactory.CreateLogger<NetworkManager>();
            _nodePool = nodePool;
            _messageHandler = new MessageHandler(this, nodePool, loggerFactory);
            _handshakeMessageHandler = new HandshakeMessageHandler(this, nodePool, loggerFactory, repo);
        }

        public ushort ListeningPort => _port;
        public IPAddress PublicIp => _publicIp;
        public bool IsDisposed => _isDisposed;
        public bool IsSyncing => _isSyncing;

        public async Task AcceptConnections(IPAddress publicIp, ushort listenPort, CancellationTokenSource cts)
        {
            _publicIp = publicIp;
            _port = listenPort;
            StartTcpListener(listenPort);

            while (!cts.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    _logger.LogInformation("Accepting incoming network connections on {0}:{1}", _publicIp, _port);
                    socket = await _listener.AcceptSocketAsync();
                    _logger.LogInformation("Received new socket connection");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    continue;
                }

                // In the first phase, check every 10s if there is a node that has a longer chain than ours.
                // After the sync completed, exit the 'syncing' state and accept new blocks and transactions.
                StartSyncTask(cts);

                var nwNode = new NetworkNode(ConnectionType.Inbound, socket);
                _nodePool.AddNetworkNode(nwNode);
                try
                {
                    // Listen for new messages with a large timeout
                    nwNode.OnMessageReceived += new Events.MessageReceivedEventHandler((sender, args) =>
                    {
                        ProcessIncomingMessage(nwNode, args.Message);
                    });
                    _ = Task.Run(() => ListenForNewMessagesContinuously(nwNode)); // Keep on listening for new messages, do not await.
                }
                catch (Exception ex)
                {
                    _logger.LogError("Disconnecting with peer {0} due to an exception: {1}", nwNode.Id, ex.Message);
                    nwNode?.Disconnect();
                }
            }

            _logger.LogWarning("Not accepting network connections anymore.");
        }

        public async Task ConnectToPeer(IPEndPoint endpoint)
        {
            var node = new NetworkNode(ConnectionType.Outbound, endpoint);
            await ConnectToPeer(node);
        }

        public void DisconnectPeer(IPEndPoint endpoint)
        {
            if (_nodePool.Contains(endpoint))
            {
                _nodePool.DisconnectConnection(endpoint);
            }
            else
            {
                _logger.LogDebug($"Tried to disconnect from {endpoint.Address}:{endpoint.Port} but that address does not exist in our pool.");
            }
        }

        public async Task ConnectToPeer(NetworkNode node)
        {
            if (node.ListenEndpoint != null && node.ListenEndpoint?.Address.MapToIPv4().ToString() == _publicIp.ToString() && node.ListenEndpoint.Port == _port)
            {
                _logger.LogDebug("Tried to connect to ourselves as a remote peer. Skipped attempt.");
                return;
            }

            if (_nodePool.Contains(node.ListenEndpoint ?? node.DirectEndpoint))
            {
                _logger.LogDebug("Tried to connect to peer: {0} on port {1}, but we are already connected.", node.DirectEndpoint.Address, node.DirectEndpoint.Port);
                return;
            }

            if (!await node.ConnectAsync())
            {
                var endpoint = node.ListenEndpoint ?? node.DirectEndpoint;
                _logger.LogError("Could not connect to peer: {0} on port {1}. The peer is not online.", endpoint.Address, endpoint.Port);
                return;
            }

            _nodePool.AddNetworkNode(node);
            try
            {
                // Send a version message
                ISerializableComponent versionPayload = new VersionPayload(BlockchainConstants.ProtocolVersion, 1, _port);
                await _handshakeMessageHandler.SendMessageToNode(node, NetworkCommand.Version, versionPayload);
                node.ProgressHandshakeStage(); // Then the handshakeMessageHandler takes care of the rest of the process
            }
            catch (Exception ex)
            {
                _logger.LogError("Couldn't connect to outgoing peer: {0}", ex.Message);
                node?.Disconnect();
            }

            // Listen for new messages with a large timeout
            node.OnMessageReceived += new Events.MessageReceivedEventHandler((sender, args) =>
            {
                ProcessIncomingMessage(node, args.Message);
            });
            _ = Task.Run(() => ListenForNewMessagesContinuously(node)); // Keep on listening for new messages, do not await.
        }

        /// <summary>
        /// Start the TCP listener so the AcceptConnections method can accept new sockets.
        /// </summary>
        /// <param name="port">The port to listen to</param>
        // Todo UPnP port forwarding
        private void StartTcpListener(ushort port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            try
            {
                _port = port;
                _listener.Start();
            }
            catch (SocketException ex)
            {
                _logger.LogError("Unable to start TCP listener: {0}", ex.Message);
            }
        }

        /// <summary>
        /// This method is called when any node receives any kind of message.
        /// Handles all incoming messages, except the handshake procedure!
        /// </summary>
        /// <param name="node">The remote node that sent the message</param>
        /// <param name="message">The received message</param>
        private async void ProcessIncomingMessage(NetworkNode node, Message message)
        {
            IPEndPoint endpoint = node.DirectEndpoint ?? node.ListenEndpoint;
            _logger.LogDebug("Received {0} message from node {1} on port {2}", message.Command, endpoint.Address.ToString(), endpoint.Port);

            if (!node.HandshakeIsCompleted)
            {
                await _handshakeMessageHandler.HandleMessage(node, message);
            }
            else
            {
                await _messageHandler.HandleMessage(node, message);
            }
        }

        /// <summary>
        /// Continuously listens for new messages using the IdleTimeout.
        /// When the node has disconnected, this listener will stop.
        /// <seealso cref="NetworkConstants.IdleTimeoutSeconds"/>
        /// </summary>
        /// <param name="node">The node to listen to</param>
        private void ListenForNewMessagesContinuously(NetworkNode node)
        {
            if (!node.IsConnected) return;

            _logger.LogDebug("Listening for new messages from node {0} on port {1} with an idle timeout of {2}sec", node.DirectEndpoint.Address.ToString(), node.DirectEndpoint.Port, NetworkConstants.IdleTimeoutSeconds);
            while (node.IsConnected)
            {
                var x = _messageHandler.ListenForNewMessage(node, new TimeSpan(0, 0, NetworkConstants.IdleTimeoutSeconds)).Result;
            }
        }
        
        private void StartSyncTask(CancellationTokenSource cts)
        {
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested && _isSyncing)
                {
                    await Task.Delay(10000);
                    try
                    {
                        var syncNode = _nodePool.GetAllNetworkNodes()
                            .Where(n => n.HandshakeIsCompleted && n.IsSyncCandidate)
                            .OrderBy(x => Guid.NewGuid()).Take(1).First();
                        _logger.LogDebug($"Attempting to sync with node {syncNode.ListenEndpoint.Address.ToString()}:{syncNode.ListenEndpoint.Port}.");

                        await _messageHandler.SendMessageToNode(syncNode, NetworkCommand.GetHeaders, null);

                    }
                    catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
                    {
                        // None found
                    }
                    _isSyncing = false;
                }
            }
            );
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _listener.Stop();
                    _nodePool.CloseAllConnections();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
