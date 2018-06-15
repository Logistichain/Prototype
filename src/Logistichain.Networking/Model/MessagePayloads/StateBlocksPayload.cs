using Logistichain.Model;
using Logistichain.Networking.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Logistichain.Networking.Model.MessagePayloads
{
    public class StateBlocksPayload : ISerializableComponent
    {
        private IEnumerable<Block> _blocks;

        internal IEnumerable<Block> Blocks => _blocks;

        public StateBlocksPayload() { }

        public StateBlocksPayload(IEnumerable<Block> blocks)
        {
            _blocks = blocks;
        }

        #region Serialization
        // Using the SingleStateBlockPayload
        public void Deserialize(BinaryReader reader)
        {
            var deserializedBlocks = new List<Block>();
            var count = reader.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                var blockPayload = new SingleStateBlockPayload();
                blockPayload.Deserialize(reader);
                deserializedBlocks.Add(blockPayload.Block);
            }
            _blocks = deserializedBlocks;
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
            writer.Write(_blocks.Count());
            foreach(var block in _blocks)
            {
                var blockPayload = new SingleStateBlockPayload(block);
                blockPayload.Serialize(writer);
            }
        }
        #endregion
    }
}
