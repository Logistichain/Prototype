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
    public class MessageHandler : AbstractMessageHandler, IMessageHandler
    {
        public MessageHandler(INetworkManager manager, NetworkNodesPool nodePool, ILoggerFactory loggerFactory)
            : base(manager, nodePool, loggerFactory) { }

        // todo Chain of Responsibility pattern, make XXMessageHandler class for each command type
        // and refactor abstractmessagehandler to a regular MessageHandlerHelper
        public async Task HandleMessage(NetworkNode node, Message msg)
        {
            try
            {
                if (msg.Command == NetworkCommand.CloseConn.ToString())
                {
                    await node.Disconnect();
                }
                else if (msg.Command == NetworkCommand.GetAddr.ToString())
                {
                    // Send our known peers
                    var addresses = new AddrPayload(_nodePool.GetAllRemoteListenEndpoints());
                    await SendMessageToNode(node, NetworkCommand.Addr, addresses);
                }
                else if (msg.Command == NetworkCommand.Addr.ToString())
                {
                    // Connect to all peers the other peer knows
                    var payload = (AddrPayload)msg.Payload;
                    foreach(IPEndPoint endpoint in payload.Endpoints)
                    {
                        await _networkManager.ConnectToPeer(endpoint);
                    }
                }
            }
            catch(Exception)
            {
                node?.Disconnect();
            }
        }
    }
}
