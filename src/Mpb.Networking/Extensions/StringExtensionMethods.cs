using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Networking.Extensions
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
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData.Trim());
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
