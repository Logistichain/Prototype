using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class AddrPayload : ISerializableComponent
    {
        private IEnumerable<IPEndPoint> _endpoints;

        internal IEnumerable<IPEndPoint> Endpoints => _endpoints;

        public AddrPayload() { }

        public AddrPayload(IEnumerable<IPEndPoint> endpoints)
        {
            _endpoints = endpoints;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            var stringLength = reader.ReadInt32();
            byte[] endpointsBytes = reader.ReadBytes(stringLength);
            var endpointsString = Encoding.BigEndianUnicode.GetString(endpointsBytes);
            _endpoints = EndpointsFromString(endpointsString);
        }

        public byte[] ToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Serialize(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            var endpointsString = EndpointsToString();
            var endpointsBytes = Encoding.BigEndianUnicode.GetBytes(endpointsString);
            writer.Write(endpointsBytes.Length);
            writer.Write(endpointsBytes);
        }

        private string EndpointsToString()
        {
            string endpoints = "";
            foreach(var endpoint in _endpoints)
            {
                endpoints += endpoint.Address.ToString() + ":" + endpoint.Port + ",";
            }

            return endpoints;
        }

        private IEnumerable<IPEndPoint> EndpointsFromString(string endpointsString)
        {
            string[] endpoints = endpointsString.Split(',');
            foreach (var endpoint in endpoints)
            {
                int idx = endpoint.LastIndexOf(':'); // Multiple : can exist with ipv6 addresses
                if (idx > 0) // This also covers the 'empty string' case
                {
                    var ip = endpoint.Substring(0, idx);
                    var port = int.Parse(endpoint.Substring(idx + 1));
                    yield return new IPEndPoint(IPAddress.Parse(ip), port);
                }
            }
        }
        #endregion
    }
}
