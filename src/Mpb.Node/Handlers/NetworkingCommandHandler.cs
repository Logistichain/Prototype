using Mpb.DAL;
using Mpb.Model;
using Mpb.Networking;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Mpb.Node.Handlers
{
    internal class NetworkingCommandHandler
    {
        internal NetworkingCommandHandler()
        {

        }

        internal ushort HandleSetPortCommand(ushort currentPort)
        {
            Console.WriteLine("Specify the new listening port. Now it's " + currentPort);
            Console.Write("> ");
            ushort newListeningPort = 0;
            var portInput = Console.ReadLine().ToLower();
            while (!ushort.TryParse(portInput, out newListeningPort) || newListeningPort > 65535)
            {
                Console.WriteLine("Invalid value. Use a positive numeric value without decimals. Maximum = 65535.");
                Console.Write("> ");
                portInput = Console.ReadLine().ToLower();
            }
            Console.WriteLine("Done. Restart the networking module to use the new port.");
            Console.Write("> ");

            return newListeningPort;
        }

        internal void HandleConnectCommand(INetworkManager networkManager)
        {
            Console.WriteLine("Specify the IP to connect to (ip:port)");
            Console.Write("> ");
            var connPortInput = Console.ReadLine().ToLower();
            try
            {
                string connectionIp = connPortInput.Split(':')[0];
                int connectPort = ushort.Parse(connPortInput.Split(':')[1]);
                networkManager.ConnectToPeer(new NetworkNode(ConnectionType.Outbound, new IPEndPoint(IPAddress.Parse(connectionIp), connectPort)));
            }
            catch (Exception)
            {
                Console.WriteLine("Something went wrong. Command aborted.");
            }
        }

        internal void HandleDisconnectCommand(INetworkManager networkManager)
        {
            Console.WriteLine("Specify the IP to disconnect from (ip:port)");
            Console.Write("> ");
            var connPortInput = Console.ReadLine().ToLower();
            try
            {
                string connectionIp = connPortInput.Split(':')[0];
                int connectPort = ushort.Parse(connPortInput.Split(':')[1]);
                networkManager.DisconnectPeer(new IPEndPoint(IPAddress.Parse(connectionIp), connectPort));
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Could not find that address in our connectionpool.");
            }
            catch (Exception)
            {
                Console.WriteLine("Something went wrong. Command aborted.");
            }
        }

        internal void HandleListPoolCommand(NetworkNodesPool nodePool)
        {
            var connectedEndpoints =  nodePool.GetAllRemoteListenEndpoints();
            Console.WriteLine($"Networking connections pool ({nodePool.Count()}):");
            foreach(var endpoint in connectedEndpoints)
            {
                // todo get the timespan from the NetworkNode object here somehow.
                Console.WriteLine($"- {endpoint.Address}:{endpoint.Port}");
            }
        }
    }
}
