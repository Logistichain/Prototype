using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Shared.Extensions
{
    /// <summary>
    /// Providing handy string extension methods.
    /// </summary>
    public static class StringExtensionMethods
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            // https://stackoverflow.com/questions/2925729/invalid-length-for-a-base-64-char-array
            var valueToDecode = base64EncodedData.Trim();
            if (valueToDecode.StartsWith("\u0003"))
            {
                valueToDecode = valueToDecode.Replace("\u0003", "");
            }

            int mod4 = valueToDecode.Length % 4;
            if (mod4 > 0)
            {
                valueToDecode += new string('=', 4 - mod4);
            }

            var base64EncodedBytes = Convert.FromBase64String(valueToDecode);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
