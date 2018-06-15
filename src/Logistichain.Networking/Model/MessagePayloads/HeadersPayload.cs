using Logistichain.Model;
using Logistichain.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Logistichain.Networking.Model.MessagePayloads
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
            var count = reader.ReadInt16();
            for(int i = 0; i < count; i++)
            {
                var hash = reader.ReadFixedString(64);
                var previoushHash = reader.ReadFixedString(64);
                var magicNumber = reader.ReadFixedString(7);
                var version = reader.ReadUInt32();
                var merkleRoot = reader.ReadFixedString(64);
                long timestamp = reader.ReadInt64();
                ulong nonce = reader.ReadUInt64();
                var signatureLength = reader.ReadInt16();
                reader.ReadByte(); // Move the reader by one byte (I don't know why, but else it's not reading the signature properly.
                var signature = reader.ReadFixedString(signatureLength);
                deserializedHeaders.Add(new BlockHeader(magicNumber, version, merkleRoot, timestamp, previoushHash).Finalize(hash, signature));
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
            if (_headers.Count() > Int16.MaxValue)
            {
                throw new InvalidDataException("Too many headers");
            }

            writer.Write((Int16)_headers.Count());
            foreach(var header in _headers)
            {
                if (header.Signature.Count() > Int16.MaxValue)
                {
                    throw new InvalidDataException("Signature field is too long");
                }

                writer.WriteFixedString(header.Hash, 64);
                writer.WriteFixedString(header.PreviousHash, 64);
                writer.WriteFixedString(header.MagicNumber, 7);
                writer.Write(header.Version);
                writer.WriteFixedString(header.MerkleRoot, 64);
                writer.Write(header.Timestamp);
                writer.Write(header.Nonce);
                writer.Write((Int16)header.Signature.Length);
                writer.Write(header.Signature);
            }
        }
        #endregion
    }
}
