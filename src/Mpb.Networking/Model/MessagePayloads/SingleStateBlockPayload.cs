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
            var blockHash = reader.ReadFixedString(64);
            var blockSignature = reader.ReadFixedString(64); // todo signature
            var previoushHash = reader.ReadFixedString(64);
            var magicNumber = reader.ReadFixedString(7);
            var blockVersion = reader.ReadUInt32();
            var merkleRoot = reader.ReadFixedString(64);
            long timestamp = reader.ReadInt64();
            ulong nonce = reader.ReadUInt64();
            // todo signature
            var txCount = reader.ReadInt32();
            var transactions = new List<AbstractTransaction>();
            for (int txi = 0; txi < txCount; txi++)
            {
                var transactionPayload = new SingleStateTransactionPayload();
                transactionPayload.Deserialize(reader);
                transactions.Add(transactionPayload.Transaction);
            }
            var header = new BlockHeader(magicNumber, blockVersion, merkleRoot, timestamp, previoushHash).Finalize(blockHash, "");
            header.Finalize(blockHash, blockSignature);
            header.IncrementNonce(nonce);
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
            writer.WriteFixedString(_block.Header.Hash, 64);
            writer.WriteFixedString(_block.Header.Signature, 64);
            writer.WriteFixedString(_block.Header.PreviousHash, 64);
            writer.WriteFixedString(_block.Header.MagicNumber, 7);
            writer.Write(_block.Header.Version);
            writer.WriteFixedString(_block.Header.MerkleRoot, 64);
            writer.Write(_block.Header.Timestamp);
            writer.Write(_block.Header.Nonce);
            // todo signature
            writer.Write(_block.Transactions.Count());
            foreach (var tx in _block.Transactions.ToList().OfType<StateTransaction>())
            {
                var encodedData = tx.Data?.Base64Encode();
                var transactionPayload = new SingleStateTransactionPayload(tx);
                transactionPayload.Serialize(writer);
            }
        }
        #endregion
    }
}
