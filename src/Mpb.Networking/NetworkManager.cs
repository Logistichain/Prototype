using Microsoft.Extensions.Logging;
using Mpb.Consensus.BlockLogic;
using Mpb.Consensus.TransactionLogic;
using Mpb.DAL;
using Mpb.Networking.Constants;
using Mpb.Networking.Events;
using Mpb.Networking.Model;
using Mpb.Networking.Model.MessagePayloads;
using Mpb.Shared.Constants;
using Mpb.Shared.Events;
using System;
using System.Collections.Concurrent;
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
        private readonly IBlockchainRepository _repo;
        private readonly string _netId;
        private ushort _port;
        private IPAddress _publicIp;
        private bool _isDisposed = false;
        private bool _isSyncing = false;
        private ILogger _logger;
        private AbstractMessageHandler _messageHandler;
        private AbstractMessageHandler _handshakeMessageHandler;
        private List<string> _relayedTransactionHashes; // To prevent endless relays
        private List<string> _relayedBlockHashes;
        private ConcurrentBag<int> _delays;

        public NetworkManager(NetworkNodesPool nodePool, ILoggerFactory loggerFactory, IBlockValidator blockValidator, IDifficultyCalculator difficultyCalculator, IBlockchainRepository repo, string netId)
        {
            _logger = loggerFactory.CreateLogger<NetworkManager>();
            _nodePool = nodePool;
            _repo = repo;
            _netId = netId;
            _relayedTransactionHashes = new List<string>();
            _relayedBlockHashes = new List<string>();
            _messageHandler = new MessageHandler(this, ConcurrentTransactionPool.GetInstance(), nodePool, difficultyCalculator, blockValidator, loggerFactory, repo, netId);
            _handshakeMessageHandler = new HandshakeMessageHandler(this, nodePool, loggerFactory, repo, netId);
            _delays = new ConcurrentBag<int>();

            EventPublisher.GetInstance().OnValidatedBlockCreated += OnValidatedBlockCreated;
            EventPublisher.GetInstance().OnValidTransactionReceived += OnValidTransactionReceived;
            // In the first phase, check every 10s if there is a node that has a longer chain than ours.
            // After the sync completed, exit the 'syncing' state and accept new blocks and transactions.
            StartSyncProcess(new CancellationTokenSource()); // todo cts
        }

        public ushort ListeningPort => _port;
        public IPAddress PublicIp => _publicIp;
        public bool IsDisposed => _isDisposed;
        public bool IsSyncing { get => _isSyncing; set => _isSyncing = value; }

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

                var nwNode = new NetworkNode(ConnectionType.Inbound, socket);
                var added = _nodePool.AddNetworkNode(nwNode);
                if (!added)
                {
                    nwNode?.Disconnect();
                    return;
                }

                try
                {
                    // Listen for new messages with a large timeout
                    nwNode.OnMessageReceived += new MessageReceivedEventHandler((sender, args) =>
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
            if (node.ListenEndpoint != null && node.ListenEndpoint.Address.MapToIPv4().ToString() == _publicIp.ToString() && node.ListenEndpoint.Port == _port)
            {
                _logger.LogDebug("Tried to connect to ourselves as a remote peer. Skipped attempt.");
                return;
            }

            if (node.ListenEndpoint != null && node.ListenEndpoint.Address.MapToIPv4().ToString().Contains("127.0.0.1") && node.ListenEndpoint.Port == _port)
            {
                _logger.LogDebug("Tried to connect to ourselves as a remote peer. Skipped attempt.");
                return;
            }

            if (_nodePool.Contains(node.ListenEndpoint ?? node.DirectEndpoint))
            {
                var endpoint = node.ListenEndpoint ?? node.DirectEndpoint;
                _logger.LogDebug("Tried to connect to peer: {0} on port {1}, but we are already connected.", endpoint.Address, endpoint.Port);
                return;
            }

            if (!await node.ConnectAsync())
            {
                var endpoint = node.ListenEndpoint ?? node.DirectEndpoint;
                _logger.LogError("Could not connect to peer: {0} on port {1}. The peer is not online.", endpoint.Address, endpoint.Port);
                return;
            }

            var added = _nodePool.AddNetworkNode(node);
            if (!added)
            {
                node?.Disconnect();
                return;
            }

            try
            {
                // Send a version message
                int currentHeight = _repo.GetChainByNetId(_netId).CurrentHeight;
                ISerializableComponent versionPayload = new VersionPayload(BlockchainConstants.ProtocolVersion, currentHeight, _port);
                await _handshakeMessageHandler.SendMessageToNode(node, NetworkCommand.Version, versionPayload);
                node.ProgressHandshakeStage(); // Then the handshakeMessageHandler takes care of the rest of the process
            }
            catch (Exception ex)
            {
                _logger.LogError("Couldn't connect to outgoing peer: {0}", ex.Message);
                node?.Disconnect();
            }

            // Listen for new messages with a large timeout
            node.OnMessageReceived += new MessageReceivedEventHandler((sender, args) =>
            {
                ProcessIncomingMessage(node, args.Message);
            });
            _ = Task.Run(() => ListenForNewMessagesContinuously(node)); // Keep on listening for new messages, do not await.
        }

        private void OnValidTransactionReceived(object sender, TransactionReceivedEventArgs eventArgs)
        {
            lock (_relayedTransactionHashes)
            {
                if (!_relayedTransactionHashes.Contains(eventArgs.Transaction.Hash))
                {
                    var txMessage = new Message(NetworkCommand.NewTransaction.ToString(), new SingleStateTransactionPayload(eventArgs.Transaction));
                    _nodePool.BroadcastMessage(txMessage);
                    _relayedTransactionHashes.Add(eventArgs.Transaction.Hash);
                }
            }
        }

        private void OnValidatedBlockCreated(object sender, BlockCreatedEventArgs eventArgs)
        {
            lock (_relayedBlockHashes)
            {
                if (!_relayedBlockHashes.Contains(eventArgs.Block.Header.Hash))
                {
                    var blockMessage = new Message(NetworkCommand.NewBlock.ToString(), new SingleStateBlockPayload(eventArgs.Block));
                    _nodePool.BroadcastMessage(blockMessage);
                    _relayedBlockHashes.Add(eventArgs.Block.Header.Hash);
                }
            }
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
                // todo this seems to be disposed when running the transactiongenerator and letting another node mine for a while..
                var x = _messageHandler.ListenForNewMessage(node, new TimeSpan(0, 0, NetworkConstants.IdleTimeoutSeconds)).Result;
            }
        }

        // todo stop this task after x time / attempts.
        internal void StartSyncProcess(CancellationTokenSource cts)
        {
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(10000);
                    var isSyncingAlready = _nodePool.GetAllNetworkNodes().Where(n => n.IsSyncingWithNode).Any();
                    if (isSyncingAlready) { continue; } // One syncnode at a time.
                    try
                    {
                        var syncNode = _nodePool.GetAllNetworkNodes()
                                .Where(n => n.HandshakeIsCompleted && n.IsSyncCandidate && !n.IsSyncingWithNode)
                                .OrderBy(x => Guid.NewGuid()).Take(1).First();
                        _logger.LogDebug($"Attempting to sync with node {syncNode.ListenEndpoint.Address.ToString()}:{syncNode.ListenEndpoint.Port}.");
                        _isSyncing = true;

                        syncNode.SetSyncStatus(SyncStatus.Initiated);
                        SyncStatusChangedEventHandler syncStatusChangedEventHandler = null;
                        syncStatusChangedEventHandler =
                            (object sender, SyncStatusChangedEventArgs ev) =>
                            {
                                var node = (NetworkNode)sender;
                                if (ev.NewStatus == SyncStatus.Succeeded)
                                {
                                    _isSyncing = false;
                                    node.IsSyncCandidate = false;
                                    node.OnSyncStatusChanged -= syncStatusChangedEventHandler;
                                }
                                else if (ev.NewStatus == SyncStatus.Failed)
                                {
                                    // Try again with another node.
                                    var endpoint = node.ListenEndpoint ?? node.DirectEndpoint;
                                    _logger.LogWarning("Failed to sync with node {0} on port {1}.", endpoint.Address.ToString(), endpoint.Port);
                                    node.IsSyncCandidate = false;
                                    node.OnSyncStatusChanged -= syncStatusChangedEventHandler;
                                }
                            };
                        syncNode.OnSyncStatusChanged += syncStatusChangedEventHandler;
                        var blockchain = _repo.GetChainByNetId(_netId);
                        var getHeadersPayload = new GetHeadersPayload(blockchain.Blocks.Last().Header.Hash);
                        await _messageHandler.SendMessageToNode(syncNode, NetworkCommand.GetHeaders, getHeadersPayload);
                    }
                    catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
                    {
                        // None found, try again later
                    }
                }
            });
        }

        internal void AddDelayRegistration(int sec)
        {
            _delays.Add(sec);
        }

        public IEnumerable<int> GetAllDelays()
        {
            return _delays;
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
