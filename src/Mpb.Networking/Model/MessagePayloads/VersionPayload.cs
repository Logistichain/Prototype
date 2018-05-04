using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mpb.Networking.Model.MessagePayloads
{
    public class VersionPayload : ISerializableComponent
    {
        private uint _protocolVersion;
        private uint _blockHeight;
        private int _listenPort;

        /// <summary>
        /// The port that the node listens to
        /// </summary>
        internal int ListenPort => _listenPort;

        /// <summary>
        /// The current block height
        /// </summary>
        internal uint BlockHeight => _blockHeight;

        /// <summary>
        /// The consensus protocol version
        /// </summary>
        internal uint ProtocolVersion => _protocolVersion;

        internal VersionPayload() { }

        public VersionPayload(uint protocolVersion, uint blockHeight, int port)
        {
            _protocolVersion = protocolVersion;
            _blockHeight = blockHeight;
            _listenPort = port;
        }

        #region Serialization
        public void Deserialize(BinaryReader reader)
        {
            _protocolVersion = reader.ReadUInt32();
            _blockHeight = reader.ReadUInt32();
            _listenPort = reader.ReadInt32();
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
            writer.Write(_blockHeight);
            writer.Write(_listenPort);
        }
        #endregion
    }
}
