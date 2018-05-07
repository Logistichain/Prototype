using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public class BlockHeader
    {
        private string _previousHash;
        private string _hash;
        private string _signature;
        private readonly string _magicNumber;
        private readonly uint _version;
        private string _merkleRoot;
        private readonly long _timestamp;
        private ulong _nonce = ulong.MinValue;

        public string PreviousHash => _previousHash;
        public string Hash => _hash;
        public string Signature => _signature;
        public string MagicNumber => _magicNumber;
        public uint Version => _version;
        public string MerkleRoot => _merkleRoot;
        public long Timestamp => _timestamp;
        public ulong Nonce => _nonce;

        public BlockHeader(string magicNumber, uint version, string merkleRoot, long timestamp, string previousBlockHash)
        {
            _magicNumber = magicNumber;
            _version = version;
            _merkleRoot = merkleRoot;
            _timestamp = timestamp;
            _previousHash = previousBlockHash;
        }

        public ulong IncrementNonce() => ++_nonce;
        public ulong IncrementNonce(ulong i) => _nonce += i;
        public bool IsFinalized() => !(String.IsNullOrWhiteSpace(_hash) || String.IsNullOrWhiteSpace(_signature));
        
        public BlockHeader Finalize(string hash, string signature)
        {
            _hash = hash;
            _signature = signature;
            return this;
        }

        public BlockHeader SetMerkleRoot(string mr)
        {
            _merkleRoot = mr;
            return this;
        }
    }
}
