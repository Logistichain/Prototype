using Microsoft.Extensions.Logging;
using Mpb.DAL;
using Mpb.Model;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;
using Mpb.Networking.Model.MessagePayloads;
using Mpb.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mpb.Networking
{
    public class HandshakeMessageHandler : AbstractMessageHandler, IMessageHandler
    {
        IBlockchainRepository _blockchainRepo;
        private readonly string _netId;

        public HandshakeMessageHandler(INetworkManager manager, NetworkNodesPool nodePool, ILoggerFactory loggerFactory, IBlockchainRepository blockchainRepo, string netId)
            : base(manager, nodePool, loggerFactory)
        {
            _blockchainRepo = blockchainRepo;
            _netId = netId;
        }
        
        public async Task HandleMessage(NetworkNode node, Message msg)
        {
            if (node.HandshakeIsCompleted) return;

            var blockchain = _blockchainRepo.GetChainByNetId(_netId);

            try
            {
                if (node.ConnectionType == ConnectionType.Inbound)
                {
                    await HandleInboundHandshakeMessage(node, msg, blockchain);
                }
                else
                {
                    await HandleOutboundHandshakeMessage(node, msg, blockchain);
                }
            }
            catch(Exception)
            {
                node?.Disconnect();
            }
        }

        private async Task HandleInboundHandshakeMessage(NetworkNode node, Message msg, Blockchain blockchain)
        {
            if (node.HandshakeStage == 0)
            {
                // Receive a version message
                // Verify if the protocol version is the same as ours and set the ListenPort
                var payload = (VersionPayload)msg.Payload;
                if (payload.ProtocolVersion != BlockchainConstants.ProtocolVersion)
                {
                    throw new ArgumentException("Mismatch in protocol version"); // todo maybe create a 'NodeException' or something
                }
                _logger.LogDebug("Accepted version from node {0} on direct port {1}. Remote listen port = {2}", node.DirectEndpoint.Address.ToString(), node.DirectEndpoint.Port, payload.ListenPort);

                node.IsSyncCandidate = payload.BlockHeight > blockchain.CurrentHeight;
                node.SetListenEndpoint(new IPEndPoint(node.DirectEndpoint.Address, payload.ListenPort));

                node.ProgressHandshakeStage();

                // Send an acknowledgement
                await SendMessageToNode(node, NetworkCommand.VerAck, null);

                // Send a version 
                ISerializableComponent versionPayload = new VersionPayload(BlockchainConstants.ProtocolVersion, blockchain.CurrentHeight, _networkManager.ListeningPort);
                await SendMessageToNode(node, NetworkCommand.Version, versionPayload);
            }
            else if (node.HandshakeStage == 1 && msg.Command == NetworkCommand.VerAck.ToString())
            {
                // And receive a version acknowledgement
                _logger.LogInformation("Successfully connected to node {0} on port {1}", node.DirectEndpoint.Address.ToString(), node.DirectEndpoint.Port);
                node.ProgressHandshakeStage();
            }
        }

        private async Task HandleOutboundHandshakeMessage(NetworkNode node, Message msg, Blockchain blockchain)
        {
            if (node.HandshakeStage == 1 && msg.Command == NetworkCommand.VerAck.ToString())
            {
                // Receive a version acknowledgement
                node.ProgressHandshakeStage();
            }
            else if (node.HandshakeStage == 2)
            {
                // Then, receive a version message
                // Verify if the protocol version is the same as ours
                var payload = (VersionPayload)msg.Payload;
                if (payload.ProtocolVersion != BlockchainConstants.ProtocolVersion)
                {
                    throw new ArgumentException("Mismatch in protocol version");
                }
                _logger.LogDebug("Accepted version from node {0} on port {1}", node.DirectEndpoint.Address.ToString(), node.DirectEndpoint.Port);
                node.IsSyncCandidate = payload.BlockHeight > blockchain.CurrentHeight;
                node.ProgressHandshakeStage();

                // Send an acknowledgement
                await SendMessageToNode(node, NetworkCommand.VerAck, null);
                _logger.LogInformation("Successfully connected to node {0} on port {1}", node.DirectEndpoint.Address.ToString(), node.DirectEndpoint.Port);

                // Send our known peers
                var addresses = new AddrPayload(_nodePool.GetAllRemoteListenEndpoints());
                await SendMessageToNode(node, NetworkCommand.Addr, addresses);

                // Request all peers that the remote node knows
                await SendMessageToNode(node, NetworkCommand.GetAddr, null);
            }
        }
    }
}
