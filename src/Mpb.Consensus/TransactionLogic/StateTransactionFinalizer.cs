using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.BlockLogic
{
    public class StateTransactionFinalizer : ITransactionFinalizer
    {
        public virtual string CalculateHash(AbstractTransaction transaction)
        {
            var txByteArray = GetTransactionBytes(transaction);
            var hashString = "";
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(txByteArray);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }
            return hashString;
        }
        
        public virtual string CreateSignature(AbstractTransaction transaction, string privKey)
        {
            // Todo dependency inject wallet mechanism to sign the transaction!
            // if coinbase, use 'ToPubKey' field
            return "txsignature";
        }
        
        //! Signature is always "" until an appropriate wallet module can be utilized.
        public void FinalizeTransaction(AbstractTransaction tx, string fromPubKey, string fromPrivKey)
        {
            if (tx.IsFinalized()) { return; }

            var txByteArray = GetTransactionBytes(tx);
            var hashString = CalculateHash(tx);
            var signature = CreateSignature(tx, fromPrivKey);

            tx.Finalize(hashString, signature);
        }

        public virtual byte[] GetTransactionBytes(AbstractTransaction transaction)
        {
            if (transaction is StateTransaction)
            {
                return GetStateTransactionBytes((StateTransaction) transaction);
            }

            throw new ArgumentException("Transaction type is not recognized");
        }

        private byte[] GetStateTransactionBytes(StateTransaction transaction)
        {
            // Accumulate all bytes (using BigEndian to support multiple platform architectures)
            List<byte[]> propertyByteList = new List<byte[]>()
            {
                Encoding.BigEndianUnicode.GetBytes(transaction.FromPubKey ?? ""),
                Encoding.BigEndianUnicode.GetBytes(transaction.ToPubKey ?? ""),
                Encoding.BigEndianUnicode.GetBytes(transaction.SkuBlockHash ?? ""),
                Encoding.BigEndianUnicode.GetBytes(transaction.SkuTxIndex.ToString()),
                Encoding.BigEndianUnicode.GetBytes(transaction.Amount.ToString()),
                Encoding.BigEndianUnicode.GetBytes(transaction.Version.ToString()),
                Encoding.BigEndianUnicode.GetBytes(transaction.Action),
                Encoding.BigEndianUnicode.GetBytes(transaction.Data ?? ""),
                Encoding.BigEndianUnicode.GetBytes(transaction.Fee.ToString())
            };

            int transactionsByteArraySize = 0;
            propertyByteList.ForEach(fieldByteArr => transactionsByteArraySize += fieldByteArr.Length);
            byte[] transactionsByteArray = new byte[transactionsByteArraySize];

            // Copy the bytes to array
            int loopedSize = 0;
            for (int i = 0; i < propertyByteList.Count; i++)
            {
                if (propertyByteList[i].Length > 0)
                {
                    Buffer.BlockCopy(propertyByteList[i], 0, transactionsByteArray, loopedSize, propertyByteList[i].Length);
                    loopedSize += propertyByteList[i].Length;
                }
            }

            return transactionsByteArray;
        }
    }
}
