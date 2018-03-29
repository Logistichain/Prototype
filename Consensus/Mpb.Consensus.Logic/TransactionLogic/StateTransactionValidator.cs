using Mpb.Consensus.Logic.BlockLogic;
using Mpb.Consensus.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.Logic.TransactionLogic
{
    public class StateTransactionValidator
    {
        private readonly TransactionByteConverter _transactionByteConverter;

        public StateTransactionValidator(TransactionByteConverter transactionByteConverter)
        {
            _transactionByteConverter = transactionByteConverter;
        }

        // Todo this is not state-specific. Apply decorator/composite pattern?
        //! This method does not build a merkle tree, but just hashes every transaction and adds them together!
        // Todo Implement a proper merkle root algorithm
        /// <summary>
        /// This method creates the hash for the entire given transaction list.
        /// If something changes, like the order or even a single bit in a transaction,
        /// the output will be completely different. This method is used to 'seal' all
        /// transactions before signing them.
        /// NOTE: This method does not build a merkle tree, but just hashes every transaction and adds them together!
        /// </summary>
        /// <param name="orderedTransactions"></param>
        /// <returns></returns>
        public virtual string CalculateMerkleRoot(ICollection<AbstractTransaction> orderedTransactions)
        {
            var hashString = "";
            List<AbstractTransaction> transactionsToLoopThrough = orderedTransactions.ToList();
            if (transactionsToLoopThrough.Count % 2 != 0)
            {
                transactionsToLoopThrough.Add(transactionsToLoopThrough.Last());
            }

            List<byte[]> hashesList = new List<byte[]>();
            foreach (AbstractTransaction t in transactionsToLoopThrough)
            {
                hashesList.Add(_transactionByteConverter.GetTransactionBytes(t));
            }

            int merkleRootArraySize = 0;
            hashesList.ForEach(hashByteArr => merkleRootArraySize += hashByteArr.Length);
            byte[] merkleRoot = new byte[merkleRootArraySize];

            // Copy the bytes to array
            int loopedSize = 0;
            for (int i = 0; i < transactionsToLoopThrough.Count; i++)
            {
                if (hashesList[i].Length > 0)
                {
                    Buffer.BlockCopy(hashesList[i], 0, merkleRoot, loopedSize, hashesList[i].Length);
                    loopedSize += hashesList[i].Length;
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(merkleRoot);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }

            return hashString;
        }
    }
}
