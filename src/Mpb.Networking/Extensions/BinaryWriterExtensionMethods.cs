using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mpb.Networking.Extensions
{
    // © NEO
    public static class BinaryWriterExtensionMethods
    {
        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > length)
                throw new ArgumentException();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > length)
                throw new ArgumentException();
            writer.Write(bytes);
            if (bytes.Length < length)
                writer.Write(new byte[length - bytes.Length]);
        }
    }
}
