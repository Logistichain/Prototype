using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.Consensus.Logic.BlockLogic
{
    /// <summary>
    /// Adapter to convert a transaction to a byte array.
    /// </summary>
    public class TransactionByteAdapter
    {
        private readonly Transaction _transaction;

        public TransactionByteAdapter(Transaction t)
        {
            _transaction = t;
        }

        public virtual byte[] GetTransactionBytes()
        {
            // Accumulate all bytes (using BigEndian to support multiple platform architectures)
            byte[] concatenatedByteArray = new byte[] { };
            List<byte[]> propertyByteList = new List<byte[]>()
            {
                //Encoding.BigEndianUnicode.GetBytes(_transaction.x) // todo this
            };

            // Copy the bytes to array
            int loopedSize = 0;
            for (int i = 0; i < propertyByteList.Count; i++)
            {
                Buffer.BlockCopy(propertyByteList[i], 0, concatenatedByteArray, loopedSize, propertyByteList[i].Length);
                loopedSize += propertyByteList[i].Length;
            }

            return concatenatedByteArray;
        }
    }
}
