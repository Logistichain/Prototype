using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public class Block
    {
        private string _previousHash;
        private string _hash;
        private string _signature;
        private readonly string _magicNumber;
        private readonly uint _version;
        private string _merkleRoot;
        private readonly long _timestamp;
        private ulong _nonce = ulong.MinValue;
        protected readonly IEnumerable<AbstractTransaction> _transactions;

        public string PreviousHash => _previousHash;
        public string Hash => _hash;
        public string Signature => _signature;
        public string MagicNumber => _magicNumber;
        public uint Version => _version;
        public string MerkleRoot => _merkleRoot;
        public long Timestamp => _timestamp;
        public ulong Nonce => _nonce;
        public IEnumerable<AbstractTransaction> Transactions => _transactions;

        public Block(string magicNumber, uint version, string merkleRoot, long timestamp, string previousBlockHash, IEnumerable<AbstractTransaction> transactions)
        {
            _magicNumber = magicNumber;
            _version = version;
            _merkleRoot = merkleRoot;
            _timestamp = timestamp;
            _previousHash = previousBlockHash;
            _transactions = transactions;
        }

        public ulong IncrementNonce() => ++_nonce;
        public ulong IncrementNonce(ulong i) => _nonce += i;
        public bool IsFinalized() => !(String.IsNullOrWhiteSpace(_hash) || String.IsNullOrWhiteSpace(_signature));
        
        public Block Finalize(string hash, string signature)
        {
            _hash = hash;
            _signature = signature;
            return this;
        }

        public Block SetMerkleRoot(string mr)
        {
            _merkleRoot = mr;
            return this;
        }
    }
}
