using Mpb.Networking.Constants;
using Mpb.Networking.Extensions;
using Mpb.Networking.Model.MessagePayloads;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Serialization inspired by NEO.
namespace Mpb.Networking.Model
{
    public class Message : ISerializableComponent
    {
        private const int PayloadMaxSize = 0x02000000; // 33554432
        private byte[] _payloadByteArray;
        internal static uint MagicNumber = 0x1234;
        internal string Command { get; private set; }
        internal uint Checksum { get; private set; }
        internal ISerializableComponent Payload { get; private set; }
        internal int Size => sizeof(uint) + 16 + sizeof(int) + sizeof(uint) + _payloadByteArray.Length;
        internal Message() { }

        internal Message(string command) : this(command, null) { }

        internal Message(string command, ISerializableComponent payload)
        {
            if (payload == null)
            {
                Payload = null;
                _payloadByteArray = new byte[0];
            } else
            {
                Payload = payload;
                _payloadByteArray = payload.ToByteArray();
            }
            Command = command;
            Checksum = GetChecksum(_payloadByteArray);
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != MagicNumber)
                throw new FormatException();
            Command = reader.ReadFixedString(16);
            uint length = reader.ReadUInt32();
            if (length > PayloadMaxSize)
                throw new FormatException();
            Checksum = reader.ReadUInt32();
            _payloadByteArray = reader.ReadBytes((int)length);
            if (GetChecksum(_payloadByteArray) != Checksum)
                throw new FormatException();

            DeserializePayloadObject();
        }

        /// <summary>
        /// Choose the right payload object according to the command
        /// </summary>
        private void DeserializePayloadObject()
        {
            using (MemoryStream ms = new MemoryStream(_payloadByteArray, false))
            using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
            {
                // Deserializing the appropriate payload object
                if (Command == NetworkCommand.Version.ToString())
                {
                    Payload = new VersionPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.Addr.ToString())
                {
                    Payload = new AddrPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.GetHeaders.ToString())
                {
                    Payload = new GetHeadersPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.Headers.ToString())
                {
                    Payload = new HeadersPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.GetBlocks.ToString())
                {
                    Payload = new GetBlocksPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.Blocks.ToString())
                {
                    Payload = new StateBlocksPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.NewBlock.ToString())
                {
                    Payload = new SingleStateBlockPayload();
                    Payload.Deserialize(br);
                }
                else if (Command == NetworkCommand.NewTransaction.ToString())
                {
                    Payload = new SingleStateTransactionPayload();
                    Payload.Deserialize(br);
                }
                else if (Command != NetworkCommand.VerAck.ToString() && Command != NetworkCommand.GetAddr.ToString()
                    && Command != NetworkCommand.CloseConn.ToString() && Command != NetworkCommand.NotFound.ToString()) // No payloads for these ones
                {
                    throw new ArgumentException("Unknown command, cannot deserialize Payload");
                }
            }
        }

        public static async Task<Message> DeserializeFromAsync(Stream stream, CancellationToken cancellationToken)
        {
            uint payload_length;
            byte[] buffer = await FillBufferAsync(stream, 28, cancellationToken);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != MagicNumber)
                    throw new FormatException();
                message.Command = reader.ReadFixedString(16);
                payload_length = reader.ReadUInt32();
                if (payload_length > PayloadMaxSize)
                    throw new FormatException();
                message.Checksum = reader.ReadUInt32();
            }
            if (payload_length > 0)
            {
                message._payloadByteArray = await FillBufferAsync(stream, (int)payload_length, cancellationToken);
                message.DeserializePayloadObject();
            }
            else
            {
                message._payloadByteArray = new byte[0];
            }
            if (GetChecksum(message._payloadByteArray) != message.Checksum)
            {
                throw new FormatException();
            }
            return message;
        }

        private static async Task<byte[]> FillBufferAsync(Stream stream, int buffer_size, CancellationToken cancellationToken)
        {
            const int MAX_SIZE = 1024;
            byte[] buffer = new byte[buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE];
            using (MemoryStream ms = new MemoryStream())
            {
                while (buffer_size > 0)
                {
                    int count = buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE;
                    count = await stream.ReadAsync(buffer, 0, count, cancellationToken);
                    if (count <= 0) throw new IOException();
                    ms.Write(buffer, 0, count);
                    buffer_size -= count;
                }
                return ms.ToArray();
            }
        }

        private static uint GetChecksum(byte[] value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(value);
                return GetUInt32(hash);
            }
        }

        // © NEO
        unsafe private static uint GetUInt32(byte[] value)
        {
            fixed (byte* pbyte = &value[0])
            {
                return *((uint*)pbyte);
            }
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
            writer.Write(MagicNumber);
            writer.WriteFixedString(Command, 16);
            writer.Write(_payloadByteArray.Length);
            writer.Write(Checksum);
            writer.Write(_payloadByteArray);
        }
    }
}
