using Mpb.Shared.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.Cryptography
{
    public static class ECDsaExtensions
    {
        public static void KeyFromString(this ECDsa dsa, string byteString)
        {
            try
            {
                byte[] keyBytes = Encoding.BigEndianUnicode.GetBytes(byteString.Base64Decode());
                var isPrivateKey = keyBytes[0] == 0xFF;
                ECParameters parameters = new ECParameters();
                parameters.Curve = ECCurve.NamedCurves.brainpoolP160r1;

                byte[] qPointX = new byte[20];
                byte[] qPointY = new byte[20];
                Buffer.BlockCopy(keyBytes, 1, qPointX, 0, 20);
                Buffer.BlockCopy(keyBytes, 21, qPointY, 0, 20);
                parameters.Q = new ECPoint();
                parameters.Q.X = qPointX;
                parameters.Q.Y = qPointY;

                if (isPrivateKey)
                {
                    byte[] d = new byte[20];
                    Buffer.BlockCopy(keyBytes, 41, d, 0, 20);
                    parameters.D = d;
                }

                dsa.ImportParameters(parameters);
            }
            catch (Exception ex)
            {
                throw new FormatException(ex.Message);
            }
        }

        public static string KeyToString(this ECDsa dsa, bool includePrivateParameters)
        {
            ECParameters parameters = dsa.ExportParameters(includePrivateParameters);
            byte[] keyBytes;
            byte[] isPrivate = includePrivateParameters ? new byte[1] { 0xFF } : new byte[1] { 0x00 };

            if (includePrivateParameters)
            {
                keyBytes = new byte[61];
                Buffer.BlockCopy(isPrivate, 0, keyBytes, 0, 1);
                Buffer.BlockCopy(parameters.Q.X, 0, keyBytes, 1, 20);
                Buffer.BlockCopy(parameters.Q.Y, 0, keyBytes, 21, 20);
                Buffer.BlockCopy(parameters.D, 0, keyBytes, 41, 20);
            }
            else
            {
                keyBytes = new byte[41];
                Buffer.BlockCopy(isPrivate, 0, keyBytes, 0, 1);
                Buffer.BlockCopy(parameters.Q.X, 0, keyBytes, 1, 20);
                Buffer.BlockCopy(parameters.Q.Y, 0, keyBytes, 21, 20);
            }

            return Encoding.BigEndianUnicode.GetString(keyBytes).Base64Encode();
        }
    }
}
