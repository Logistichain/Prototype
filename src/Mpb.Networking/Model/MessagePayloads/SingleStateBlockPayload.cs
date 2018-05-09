using Mpb.Model;
using Mpb.Networking.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class SingleStateBlockPayload : ISerializableComponent
    {
        private Block _block;

        internal Block Block => _block;

        public SingleStateBlockPayload() { }

        public SingleStateBlockPayload(Block block)
        {
            _block = block;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            var hash = reader.ReadFixedString(32);
            var previoushHash = reader.ReadFixedString(32);
            var magicNumber = reader.ReadFixedString(7);
            var blockVersion = reader.ReadUInt32();
            var merkleRoot = reader.ReadFixedString(32);
            long timestamp = reader.ReadInt64();
            ulong nonce = reader.ReadUInt64();
            // todo signature
            var txCount = reader.ReadInt32();
            var transactions = new List<AbstractTransaction>();
            for(int txi = 0; txi < txCount; txi++)
            {
                var fromPubKey = reader.ReadFixedString(32); // Todo wallet implementation
                var toPubKey = reader.ReadFixedString(32); // Todo wallet implementation
                var skuBlockHash = reader.ReadFixedString(32);
                var skuTxIndex = reader.ReadInt32();
                var amount = reader.ReadUInt32();
                var txVersion = reader.ReadUInt32();
                var action = reader.ReadFixedString(16);
                var fee = reader.ReadUInt32();
                var dataSize = reader.ReadInt32();
                var data = reader.ReadFixedString(dataSize);
            }
            var header = new BlockHeader(magicNumber, blockVersion, merkleRoot, timestamp, previoushHash).Finalize(hash, "");
            _block = new Block(header, transactions);
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
            writer.WriteFixedString(_block.Header.Hash, 32);
            writer.WriteFixedString(_block.Header.PreviousHash, 32);
            writer.WriteFixedString(_block.Header.MagicNumber, 7);
            writer.Write(_block.Header.Version);
            writer.WriteFixedString(_block.Header.MerkleRoot, 32);
            writer.Write(_block.Header.Timestamp);
            writer.Write(_block.Header.Nonce);
            // todo signature
            writer.Write(_block.Transactions.Count());
            foreach (var tx in _block.Transactions.ToList().OfType<StateTransaction>())
            {
                writer.WriteFixedString(tx.FromPubKey, 32); // Todo wallet implementation
                writer.WriteFixedString(tx.ToPubKey, 32); // Todo wallet implementation
                writer.WriteFixedString(tx.SkuBlockHash, 32);
                writer.Write(tx.SkuTxIndex);
                writer.Write(tx.Amount);
                writer.Write(tx.Version);
                writer.WriteFixedString(tx.Action, 16);
                writer.Write(tx.Fee);
                writer.Write(tx.Data.Length);
                writer.Write(tx.Data);
            }
        }
        #endregion
    }
}
