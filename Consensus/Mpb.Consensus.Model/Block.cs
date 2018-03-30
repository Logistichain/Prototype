using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class Block
    {
        private string _hash;
        private readonly string _magicNumber;
        private readonly uint _version;
        private readonly string _merkleRoot;
        private readonly long _timestamp;
        private ulong _nonce = ulong.MinValue;
        protected readonly IEnumerable<AbstractTransaction> _transactions;

        public string Hash => _hash;
        public string MagicNumber => _magicNumber;
        public uint Version => _version;
        public string MerkleRoot => _merkleRoot;
        public long Timestamp => _timestamp;
        public ulong Nonce => _nonce;
        public IEnumerable<AbstractTransaction> Transactions => _transactions;

        public Block(string magicNumber, uint version, string merkleRoot, long timestamp, IEnumerable<AbstractTransaction> transactions)
        {
            _magicNumber = magicNumber;
            _version = version;
            _merkleRoot = merkleRoot;
            _timestamp = timestamp;
            _transactions = transactions;
        }

        public ulong IncrementNonce() => ++_nonce;
        public ulong IncrementNonce(ulong i) => _nonce += i;

        public Block SetHash(string hash)
        {
            _hash = hash;
            return this;
        }
    }
}
