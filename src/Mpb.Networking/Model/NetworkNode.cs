using Mpb.Networking.Constants;
using Mpb.Networking.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mpb.Networking.Model
{
    /// <summary>
    /// Network node to send and receive messages from.
    /// Heavily inspired by NEO's codebase, so credits to them!
    /// https://github.com/neo-project/neo/blob/ee60199df8010e408ebf4e5aa438d19d15251c36/neo/Network/TcpRemoteNode.cs
    /// </summary>
    public class NetworkNode : IDisposable
    {
        private Guid _id;
        private IPEndPoint _directEndpoint;
        private IPEndPoint _listenEndpoint;
        private Socket _socket;
        private NetworkStream _stream;
        private int _isDisposed = 0;
        internal event MessageReceivedEventHandler OnMessageReceived;
        public event DisconnectedEventHandler OnDisconnected;
        internal event ListenerEndpointChangedEventHandler OnListenerEndpointChanged;
        internal event SyncStatusChangedEventHandler OnSyncStatusChanged;
        private ConnectionType _connectionType;
        private int _handshakeStage = 0;
        private DateTime _connectionEstablishedAt;
        private bool _isSyncCandidate = false;
        private SyncStatus _syncStatus = SyncStatus.NotSyncing;

        public NetworkNode(ConnectionType direction, Socket socket)
        {
            _id = Guid.NewGuid();
            _connectionType = direction;
            _socket = socket;
            if (IsConnected)
            {
                OnConnected();
            }
        }

        public NetworkNode(ConnectionType direction, IPEndPoint nodeListenerEndpoint)
            : this(direction, new Socket(nodeListenerEndpoint.Address.IsIPv4MappedToIPv6 ? AddressFamily.InterNetwork : nodeListenerEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
            _listenEndpoint = nodeListenerEndpoint;
        }

        /// <summary>
        /// The identifier for this object.
        /// This Guid is generated when calling the constructor.
        /// </summary>
        public Guid Id => _id;

        /// <summary>
        /// This is the dedicated connection between us and the network node.
        /// The IP address must match with the listener endpoint address.
        /// </summary>
        public IPEndPoint DirectEndpoint { get => _directEndpoint; set => _directEndpoint = value.Address == _directEndpoint.Address ? value : _directEndpoint; }

        /// <summary>
        /// Send a message to this endpoint to initiate a codewith the remote node.
        /// </summary>
        public IPEndPoint ListenEndpoint => _listenEndpoint;

        /// <summary>
        /// Whether the 'version' messages were exchanged, the protocol versions match
        /// and the 'verack' messages have been sent from both parties and we received
        /// the 'addr' message so the ListenerEndpoint is known.
        /// </summary>
        public bool HandshakeIsCompleted => (_connectionType == ConnectionType.Inbound && _handshakeStage == 2)
                                            || (_connectionType == ConnectionType.Outbound && _handshakeStage == 3);

        public int HandshakeStage => _handshakeStage;

        /// <summary>
        /// When the networksocket is connected or not.
        /// </summary>
        public bool IsConnected => _socket != null ? _socket.Connected : false;

        public bool IsDisposed => _isDisposed > 0;

        /// <summary>
        /// Whether this node has a higher block height than our local chain.
        /// </summary>
        public bool IsSyncCandidate { get => _isSyncCandidate; set => _isSyncCandidate = value; }

        /// <summary>
        /// This is true when we are synchronizing with this node, to prevent processing random 'Headers' messages.
        /// </summary>
        public bool IsSyncingWithNode => _syncStatus == SyncStatus.Initiated || _syncStatus == SyncStatus.InProgress;

        /// <summary>
        /// The status of how the synchronization with this node is going.
        /// </summary>
        public SyncStatus SyncStatus { get => _syncStatus; }

        public ConnectionType ConnectionType => _connectionType;

        /// <summary>
        /// The datetime when the 'OnConnect' method in this class was called.
        /// This value might change when the connection resets.
        /// </summary>
        public DateTime ConnectionEstablishedAt => _connectionEstablishedAt;

        internal void SetSyncStatus(SyncStatus newStatus)
        {
            // The OnSyncStatusChanged listener sets the IsSyncCandidate to false
            OnSyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(_syncStatus, newStatus));
            _syncStatus = newStatus;
        }

        internal void ProgressHandshakeStage()
        {
            lock (this)
            {
                if (!HandshakeIsCompleted)
                    _handshakeStage++;
            }
        }

        /// <summary>
        /// Connect to the node.
        /// </summary>
        /// <returns>Whether connecting succeeded or not</returns>
        //todo prevent connecting to localhost
        internal async Task<bool> ConnectAsync()
        {
            if (IsConnected) return true;

            try
            {
                await _socket.ConnectAsync(_listenEndpoint.Address, _listenEndpoint.Port);
                OnConnected();
            }
            catch (SocketException)
            {
                await Disconnect();
                return false;
            }
            return true;
        }

        internal async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (_isDisposed > 0) throw new ObjectDisposedException("NetworkNode");
            if (!IsConnected) await ConnectAsync();

            CancellationTokenSource source = new CancellationTokenSource(timeout);
            //Stream.ReadAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => { Dispose(); });
            try
            {
                var receivedMessage = await Message.DeserializeFromAsync(_stream, source.Token);
                OnMessageReceived?.Invoke(this, new MessageEventArgs(receivedMessage));
                return receivedMessage;
            }
            catch (ArgumentException ex)
            {
                // todo logging
                Console.Write("Failed to read message: " + ex.Message);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is OperationCanceledException)
            {
                await Disconnect();
            }
            finally
            {
                source.Dispose();
            }
            return null;
        }

        internal void SetListenEndpoint(IPEndPoint listenEndpoint)
        {
            OnListenerEndpointChanged?.Invoke(this, new ListenerEndpointChangedEventArgs(_listenEndpoint, listenEndpoint));
            _listenEndpoint = listenEndpoint;
        }

        internal async Task<bool> SendMessageAsync(Message message)
        {
            if (_isDisposed > 0) throw new ObjectDisposedException("NetworkNode");
            if (!IsConnected)
            {
                if (!await ConnectAsync()) // Connect failed
                {
                    return false;
                }
            }

            byte[] buffer = message.ToByteArray();
            CancellationTokenSource source = new CancellationTokenSource(30000);
            //Stream.WriteAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => { Dispose(); });
            try
            {
                await _stream.WriteAsync(buffer, 0, buffer.Length, source.Token);
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is IOException || ex is OperationCanceledException)
            {
                await Disconnect();
            }
            finally
            {
                source.Dispose();
            }
            return false;
        }

        public async Task Disconnect()
        {
            if (_socket.Connected)
            {
                await SendMessageAsync(new Message(NetworkCommand.CloseConn.ToString()));
            }
            OnDisconnected?.Invoke(this);
            _socket?.Dispose();
            _stream?.Dispose();
        }

        private void OnConnected()
        {
            IPEndPoint directEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            _directEndpoint = new IPEndPoint(directEndPoint.Address.MapToIPv6(), directEndPoint.Port);
            _stream = new NetworkStream(_socket);
            _connectionEstablishedAt = DateTime.Now;
        }

        #region Dispose        
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed != 1)
            {
                Disconnect();
                _isDisposed = 1;
            }
        }

        ~NetworkNode()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public override string ToString()
        {
            return _id.ToString();
        }

        public override bool Equals(object obj)
        {
            return ToString() == obj.ToString();
        }
    }
}
