using System;
using System.Threading.Tasks;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;

namespace Mpb.Networking
{
    public interface IMessageHandler
    {
        Task<Message> ExpectMessageFromNode(NetworkNode node, NetworkCommand expectedCommand);
        Task HandleMessage(NetworkNode node, Message msg);
        Task<Message> ListenForNewMessage(NetworkNode node, TimeSpan timeout);
        Task SendMessageToNode(NetworkNode node, NetworkCommand command, ISerializableComponent payload);
    }
}