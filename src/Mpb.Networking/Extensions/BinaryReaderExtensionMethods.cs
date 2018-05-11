using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mpb.Networking.Extensions
{
    // © NEO
    public static class BinaryReaderExtensionMethods
    {
        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            byte[] data = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(data.TakeWhile(p => p != 0).ToArray());
        }
    }
}
