using Microsoft.Extensions.Logging;
using Logistichain.Networking.Constants;
using Logistichain.Networking.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Logistichain.Networking.Events;

namespace Logistichain.Networking
{
    public sealed class NetworkNodesPool : IDisposable
    {
        private ConcurrentDictionary<string, NetworkNode> _nodesPool;
        private static volatile NetworkNodesPool _instance;
        private static object _threadLock = new Object();
        private bool _isDisposed = false;
        private ILogger _logger;

        private NetworkNodesPool(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NetworkNodesPool>();
            _nodesPool = new ConcurrentDictionary<string, NetworkNode>();
        }

        public int Count() => _nodesPool.Count;

        /// <summary>
        /// Gets the singleton instance for this object.
        /// </summary>
        /// <returns></returns>
        public static NetworkNodesPool GetInstance(ILoggerFactory loggerFactory)
        {
            if (_instance == null)
            {
                lock (_threadLock)
                {
                    if (_instance == null)
                        _instance = new NetworkNodesPool(loggerFactory);
                }
            }

            return _instance;
        }

        /// <summary>
        /// Add a network node and connects if it hasn't been done already.
        /// Duplicate network nodes cannot exist in the pool.
        /// Maximum of x connected nodes are allowed.
        /// <seealso cref="NetworkConstants.MaxConcurrentConnections"/>
        /// </summary>
        /// <param name="node">The node to add</param>
        public bool AddNetworkNode(NetworkNode node)
        {
            var added = false;
            if (!node.IsDisposed && Count() < NetworkConstants.MaxConcurrentConnections)
            {
                lock(_nodesPool)
                {
                    var endpoint = node.ListenEndpoint ?? node.DirectEndpoint;
                    if (!_nodesPool.Values.Where(
                        n => (n.ListenEndpoint != null && n.ListenEndpoint.Address.ToString().Contains(endpoint.Address.ToString()))
                        || (n.DirectEndpoint != null && n.DirectEndpoint.Address.ToString().Contains(endpoint.Address.ToString()))).Any())
                    {
                        added = _nodesPool.TryAdd(node.ToString(), node);
                    }
                }
            }

            if (added)
            {
                _logger.LogDebug("Added node {0} to pool. {0}/{1} connections.", node.Id, Count(), NetworkConstants.MaxConcurrentConnections);
                // Subscribe to events
                node.OnDisconnected += Node_OnDisconnected;
                node.OnListenerEndpointChanged += Node_OnListenerEndpointChanged;
            }

            return added;
        }

        /// <summary>
        /// Loops through the entire nodes pool and returns the ListenEndpoint
        /// for all nodes who had a successful handshake with us.
        /// This collection is mainly used for the 'addr' message.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPEndPoint> GetAllRemoteListenEndpoints()
        {
            return _nodesPool.Where(n => n.Value.HandshakeIsCompleted).Select(n => n.Value.ListenEndpoint);
        }

        /// <summary>
        /// Checks if this pool contains the endpoint by searching through all
        /// DirectEndpoints and ListenerEndpoints of all nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Contains(IPEndPoint endpoint)
        {
            try
            {
                GetNodeByEndpoint(endpoint);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns all network nodes by reference.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<NetworkNode> GetAllNetworkNodes()
        {
            return _nodesPool.Values;
        }

        /// <summary>
        /// Throws InvalidOperationException when the endpoint doesn't exist.
        /// </summary>
        /// <param name="endpoint"></param>
        internal async void DisconnectConnection(IPEndPoint endpoint)
        {
            var node = GetNodeByEndpoint(endpoint);
            await node.Disconnect();
        }

        /// <summary>
        /// Broadcasts a message to all verified peers
        /// </summary>
        /// <param name="m">The message to send</param>
        internal void BroadcastMessage(Message m)
        {
            lock (_nodesPool)
            {
                foreach (var node in _nodesPool.Where(n => n.Value.HandshakeIsCompleted))
                {
                    _ = node.Value.SendMessageAsync(m);
                }
            }
        }

        internal void CloseAllConnections()
        {
            foreach (var node in _nodesPool)
            {
                node.Value.Dispose();
            }
        }

        private NetworkNode GetNodeByEndpoint(IPEndPoint endpoint)
        {
            return _nodesPool
                .Where(n =>
                    (
                    n.Value.DirectEndpoint.Address.MapToIPv4().ToString() == endpoint.Address.MapToIPv4().ToString()
                    && n.Value.DirectEndpoint.Port.Equals(endpoint.Port)
                    )
                    ||
                    (
                    n.Value.ListenEndpoint != null
                    && n.Value.ListenEndpoint.Address.MapToIPv4().ToString() == endpoint.Address.MapToIPv4().ToString()
                    && n.Value.ListenEndpoint.Port.Equals(endpoint.Port)
                    )
                )
                .First().Value;
        }

        private void Node_OnListenerEndpointChanged(object sender, ListenerEndpointChangedEventArgs ev)
        {
            // Using this event to prevent maintaining multiple incoming direct connections from one IP.
            var existingNodes = _nodesPool
                                    .Where(n =>
                                        n.Value.ListenEndpoint != null
                                        && n.Value.ListenEndpoint.Address.Equals(ev.NewEndpoint.Address)
                                        && n.Value.ListenEndpoint.Port == ev.NewEndpoint.Port
                                        )
                                    .ToList();
            if (existingNodes != null)
            {
                for (int i = 0; i < existingNodes.Count(); i++)
                {
                    existingNodes[i].Value.Disconnect().Start();
                }
            }
        }

        private void Node_OnDisconnected(object sender)
        {
            var node = (NetworkNode)sender;
            _logger.LogInformation("Node {0}:{1} disconnected.", node.ListenEndpoint.Address.ToString(), node.ListenEndpoint.Port);
            lock (_nodesPool)
            {
                var removed = _nodesPool.TryRemove(node.ToString(), out var ignored);
                if (removed)
                {
                    _logger.LogDebug("Removed node {0}:{1} from pool.", node.ListenEndpoint.Address.ToString(), node.ListenEndpoint.Port);
                }
                else
                {
                    _logger.LogError("Unable to remove node {0} {1}:{2} from pool. Maybe it was removed already?", node.Id, node.ListenEndpoint.Address.ToString(), node.ListenEndpoint.Port);
                }
            }
            node.OnListenerEndpointChanged -= Node_OnListenerEndpointChanged;
            node.OnDisconnected -= Node_OnDisconnected;
        }

        #region Dispose
        void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    CloseAllConnections();
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
