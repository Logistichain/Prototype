using Mpb.Model;
using Mpb.Networking.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class GetBlocksPayload : ISerializableComponent
    {
        private IEnumerable<string> _hashes;

        internal IEnumerable<string> Headers => _hashes;

        public GetBlocksPayload() { }

        public GetBlocksPayload(IEnumerable<string> hashes)
        {
            _hashes = hashes;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            var deserializedHashes = new List<string>();
            var count = reader.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                var hash = reader.ReadFixedString(32);
                deserializedHashes.Add(hash);
            }
            _hashes = deserializedHashes;
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
            writer.Write(_hashes.Count());
            foreach(var hash in _hashes)
            {
                writer.WriteFixedString(hash, 32);
            }
        }
        #endregion
    }
}
