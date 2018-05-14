using Mpb.Model;
using Mpb.Networking.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class SingleStateTransactionPayload : ISerializableComponent
    {
        private AbstractTransaction _transaction;

        // Using AbstractTransaction to make the transition to another tx type much easier
        internal AbstractTransaction Transaction => _transaction;

        public SingleStateTransactionPayload() { }

        public SingleStateTransactionPayload(AbstractTransaction block)
        {
            _transaction = block;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            var txHash = reader.ReadFixedString(64);
            var txSignature = reader.ReadFixedString(64); // todo signature
            var fromPubKey = reader.ReadFixedString(64); // Todo wallet implementation
            var toPubKey = reader.ReadFixedString(64); // Todo wallet implementation
            var skuBlockHash = reader.ReadFixedString(64);
            var skuTxIndex = reader.ReadInt32();
            var amount = reader.ReadUInt32();
            var txVersion = reader.ReadUInt32();
            var action = reader.ReadFixedString(16);
            var fee = reader.ReadUInt32();
            var dataSize = reader.ReadInt32();
            var data = "";
            if (dataSize > 0)
            {
                // Slide the reading window +1 because it moved one byte somehow.. I don't know why.
                data = reader.ReadFixedString(dataSize + 1);
                data = data.Substring(1);
                data = data.Base64Decode();
            }

            var tx = new StateTransaction(
                    string.IsNullOrEmpty(fromPubKey) ? null : fromPubKey,
                    string.IsNullOrEmpty(toPubKey) ? null : toPubKey,
                    string.IsNullOrEmpty(skuBlockHash) ? null : skuBlockHash,
                    skuTxIndex,
                    amount,
                    txVersion,
                    action,
                    data,
                    fee);
            tx.Finalize(txHash, txSignature);
            _transaction = tx;
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
            var tx = (StateTransaction)_transaction;
            var encodedData = tx.Data?.Base64Encode();
            writer.WriteFixedString(tx.Hash, 64);
            writer.WriteFixedString(tx.Signature, 64); // Todo signature
            writer.WriteFixedString(tx.FromPubKey ?? "", 64); // Todo wallet implementation
            writer.WriteFixedString(tx.ToPubKey ?? "", 64); // Todo wallet implementation
            writer.WriteFixedString(tx.SkuBlockHash ?? "", 64);
            writer.Write(tx.SkuTxIndex);
            writer.Write(tx.Amount);
            writer.Write(tx.Version);
            writer.WriteFixedString(tx.Action, 16);
            writer.Write(tx.Fee);
            writer.Write(encodedData != null ? encodedData.Length : 0);
            if (encodedData != null)
            {
                writer.Write(encodedData);
            }
        }
        #endregion
    }
}
