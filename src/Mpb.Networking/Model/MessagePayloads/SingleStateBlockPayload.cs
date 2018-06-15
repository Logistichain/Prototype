using Mpb.Model;
using Mpb.Networking.Extensions;
using Mpb.Shared.Extensions;
using System;
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
            var blockSignatureLength = reader.ReadInt16();
            reader.ReadByte(); // Move the reader by one byte (I don't know why, but else it's not reading the signature properly.
            var blockSignature = reader.ReadFixedString(blockSignatureLength);
            var previoushHash = reader.ReadFixedString(64);
            var magicNumber = reader.ReadFixedString(7);
            var blockVersion = reader.ReadUInt32();
            var merkleRoot = reader.ReadFixedString(64);
            long timestamp = reader.ReadInt64();
            ulong nonce = reader.ReadUInt64();
            var signatureLength = reader.ReadInt16();
            reader.ReadByte(); // Move the reader by one byte (I don't know why, but else it's not reading the signature properly.
            var signature = reader.ReadFixedString(signatureLength);

            var transactionsPayload = new StateTransactionsPayload();
            transactionsPayload.Deserialize(reader);
            var header = new BlockHeader(magicNumber, blockVersion, merkleRoot, timestamp, previoushHash).Finalize(blockHash, signature);
            header.Finalize(blockHash, blockSignature);
            header.IncrementNonce(nonce);
            _block = new Block(header, transactionsPayload.Transactions);
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
            if (_block.Transactions.Count() > Int16.MaxValue)
            {
                throw new InvalidDataException("Too many transactions");
            }

            writer.WriteFixedString(_block.Header.Hash, 64);
            writer.Write((Int16)_block.Header.Signature.Length);
            writer.Write(_block.Header.Signature);
            writer.WriteFixedString(_block.Header.PreviousHash, 64);
            writer.WriteFixedString(_block.Header.MagicNumber, 7);
            writer.Write(_block.Header.Version);
            writer.WriteFixedString(_block.Header.MerkleRoot, 64);
            writer.Write(_block.Header.Timestamp);
            writer.Write(_block.Header.Nonce);
            writer.Write((Int16)_block.Header.Signature.Length);
            writer.Write(_block.Header.Signature);

            var transactionsPayload = new StateTransactionsPayload(_block.Transactions);
            transactionsPayload.Serialize(writer);
        }
        #endregion
    }
}
