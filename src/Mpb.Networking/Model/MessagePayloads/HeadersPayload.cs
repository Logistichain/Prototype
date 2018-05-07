using Mpb.Model;
using Mpb.Networking.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class HeadersPayload : ISerializableComponent
    {
        private IEnumerable<BlockHeader> _headers;

        internal IEnumerable<BlockHeader> Headers => _headers;

        public HeadersPayload() { }

        public HeadersPayload(IEnumerable<BlockHeader> headers)
        {
            _headers = headers;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            List<BlockHeader> deserializedHeaders = new List<BlockHeader>();
            var count = reader.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                var hash = reader.ReadFixedString(32);
                var previoushHash = reader.ReadFixedString(32);
                var magicNumber = reader.ReadFixedString(7);
                var version = reader.ReadUInt32();
                var merkleRoot = reader.ReadFixedString(32);
                long timestamp = reader.ReadInt64();
                ulong nonce = reader.ReadUInt64();
                // todo signature
                deserializedHeaders.Add(new BlockHeader(magicNumber, version, merkleRoot, timestamp, previoushHash).Finalize(hash, ""));
            }
            _headers = deserializedHeaders;
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
            writer.Write(_headers.Count());
            foreach(var header in _headers)
            {
                writer.WriteFixedString(header.Hash, 32);
                writer.WriteFixedString(header.PreviousHash, 32);
                writer.WriteFixedString(header.MagicNumber, 7);
                writer.Write(header.Version);
                writer.WriteFixedString(header.MerkleRoot, 32);
                writer.Write(header.Timestamp);
                writer.Write(header.Nonce);
                // todo signature
            }
        }
        #endregion
    }
}
