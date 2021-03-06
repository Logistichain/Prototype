﻿using Logistichain.Model;
using Logistichain.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Logistichain.Networking.Model.MessagePayloads
{
    public class StateTransactionsPayload : ISerializableComponent
    {
        private IEnumerable<AbstractTransaction> _transactions;

        internal IEnumerable<AbstractTransaction> Transactions => _transactions;

        public StateTransactionsPayload() { }

        public StateTransactionsPayload(IEnumerable<AbstractTransaction> transactions)
        {
            _transactions = transactions;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            var deserializedTransactions = new List<AbstractTransaction>();
            var count = reader.ReadInt16();
            for(int i = 0; i < count; i++)
            {
                var txPayload = new SingleStateTransactionPayload();
                txPayload.Deserialize(reader);
                deserializedTransactions.Add(txPayload.Transaction);
            }
            _transactions = deserializedTransactions;
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
            if (_transactions.Count() > Int16.MaxValue)
            {
                throw new InvalidDataException("Too many transactions");
            }

            writer.Write((Int16)_transactions.Count());
            foreach(var tx in _transactions)
            {
                var transactionPayload = new SingleStateTransactionPayload(tx);
                transactionPayload.Serialize(writer);
            }
        }
        #endregion
    }
}
