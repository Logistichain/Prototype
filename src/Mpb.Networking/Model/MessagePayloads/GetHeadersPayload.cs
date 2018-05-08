﻿using Mpb.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class GetHeadersPayload : ISerializableComponent
    {
        private uint _protocolVersion;
        private string _highestHeightHash;
        private string _stoppingHash;

        /// <summary>
        /// The hash value that should be the last in the receiving sequence.
        /// This hash can be all zeroes, then it will return the max amount of 
        /// hashes per message.
        /// <seealso cref="Constants.NetworkConstants"/>
        /// </summary>
        internal string StoppingHash => _stoppingHash;

        /// <summary>
        /// The hash of the last block we have.
        /// </summary>
        internal string HighestHeightHash => _highestHeightHash;

        /// <summary>
        /// Our consensus protocol version.
        /// </summary>
        internal uint ProtocolVersion => _protocolVersion;
        
        public GetHeadersPayload(uint protocolVersion, string highestHeightHash, string stoppingHash)
        {
            _protocolVersion = protocolVersion;
            _highestHeightHash = highestHeightHash;
            _stoppingHash = stoppingHash;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            _protocolVersion = reader.ReadUInt32();
            _highestHeightHash = reader.ReadFixedString(32);
            _stoppingHash = reader.ReadFixedString(32);
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
            writer.Write(_protocolVersion);
            writer.WriteFixedString(_highestHeightHash, 32);
            writer.WriteFixedString(_stoppingHash, 32);
        }
        #endregion
    }
}