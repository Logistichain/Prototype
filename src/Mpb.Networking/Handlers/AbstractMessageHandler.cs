using Microsoft.Extensions.Logging;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;
using Mpb.Networking.Model.MessagePayloads;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mpb.Networking
{
    public abstract class AbstractMessageHandler
    {
        protected readonly INetworkManager _networkManager;
        protected readonly NetworkNodesPool _nodePool;
        protected readonly ILogger _logger;

        protected AbstractMessageHandler(INetworkManager manager, NetworkNodesPool nodePool, ILoggerFactory loggerFactory)
        {
            _networkManager = manager;
            _nodePool = nodePool;
            _logger = loggerFactory.CreateLogger<MessageHandler>();
        }

        public abstract Task HandleMessage(NetworkNode node, Message msg);
        
        /// <summary>
        /// Helper method to reduce duplicate LOC
        /// </summary>
        /// <param name="node">The node to send the message to</param>
        /// <param name="command">The command</param>
        /// <param name="payload">The payload that corresponds with the command</param>
        public async Task SendMessageToNode(NetworkNode node, NetworkCommand command, ISerializableComponent payload)
        {
            //await Task.Delay(1000);
            IPEndPoint endpoint = node.DirectEndpoint ?? node.ListenEndpoint;
            var msg = new Message(command.ToString(), payload);
            await node.SendMessageAsync(msg);
            _logger.LogDebug("Sent {0} message to node {1} on port {2}", command.ToString(), endpoint.Address.ToString(), endpoint.Port);
        }

        /// <summary>
        /// Helper method to reduce duplicate LOC
        /// </summary>
        /// <param name="node">The node to receive the message from</param>
        public async Task<Message> ExpectMessageFromNode(NetworkNode node, NetworkCommand expectedCommand)
        {
            await Task.Delay(1000);
            var msg = await ListenForNewMessage(node, new TimeSpan(0, 0, NetworkConstants.ExpectMsgTimeoutSeconds));
            if (msg.Command != expectedCommand.ToString())
            {
                throw new ArgumentException("Expected command to be " + expectedCommand.ToString() + " but received " + msg.Command);
            }

            return msg;
        }

        /// <summary>
        /// Helper method that catches new messages (and handles connection exceptions).
        /// When an exception occurs, we will disconnect with the peer.
        /// </summary>
        /// <param name="node">The node to listen to</param>
        /// <param name="timeout">The message timeout to maintain. Disconnect when timeout has been exceeded.</param>
        /// <returns>The message that we received from the node</returns>
        public async Task<Message> ListenForNewMessage(NetworkNode node, TimeSpan timeout)
        {
            try
            {
                return await node.ReceiveMessageAsync(timeout);
            }
            catch (Exception)
            {
                await node?.Disconnect();
            }

            return null;
        }
    }
}
