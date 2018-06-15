using Logistichain.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Logistichain.Networking.Model.MessagePayloads
{
    public class GetHeadersPayload : ISerializableComponent
    {
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

        public GetHeadersPayload() { }

        public GetHeadersPayload(string highestHeightHash) : this(highestHeightHash, "0000000000000000000000000000000000000000000000000000000000000000") { }
        
        public GetHeadersPayload(string highestHeightHash, string stoppingHash)
        {
            _highestHeightHash = highestHeightHash;
            _stoppingHash = stoppingHash;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            _highestHeightHash = reader.ReadFixedString(64);
            _stoppingHash = reader.ReadFixedString(64);
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
            writer.WriteFixedString(_highestHeightHash, 64);
            writer.WriteFixedString(_stoppingHash, 64);
        }
        #endregion
    }
}
