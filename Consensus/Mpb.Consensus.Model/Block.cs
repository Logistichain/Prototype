﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class Block
    {
        private readonly string _magicNumber;
        private readonly int _version;
        private readonly string _merkleRoot;
        private readonly long _timestamp;
        private long _difficultyTarget;
        private ulong _nonce = ulong.MinValue;
        private readonly IEnumerable<Transaction> _transactions;

        public string MagicNumber => _magicNumber;
        public int Version => _version;
        public string MerkleRoot => _merkleRoot;
        public long Timestamp => _timestamp;
        public long DifficultyTarget => _difficultyTarget;
        public ulong Nonce => _nonce;
        public IEnumerable<Transaction> Transactions => _transactions;

        public Block(string magicNumber, int version, string merkleRoot, long timestamp, IEnumerable<Transaction> transactions)
        {
            _magicNumber = magicNumber;
            _version = version;
            _merkleRoot = merkleRoot;
            _timestamp = timestamp;
            _transactions = transactions;
        }

        public ulong IncrementNonce() => ++_nonce;
    }
}
