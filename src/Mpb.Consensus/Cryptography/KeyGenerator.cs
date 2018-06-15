using Mpb.Shared.Extensions;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mpb.Consensus.Cryptography
{
    // © Sander Gerz https://blog.todotnet.com/2018/02/public-private-keys-and-signing/
    public class KeyGenerator : IKeyGenerator
    {
        public void GenerateKeys(out string publicKey, out string privateKey)
        {
            string privKey = new BigInteger(2048, new Random()).ToString(16);
            privateKey = privKey;
            publicKey = GeneratePublicKey(privKey);
        }

        public string GeneratePublicKey(string privateKey)
        {
            if (String.IsNullOrWhiteSpace(privateKey))
            {
                throw new FormatException("Empty private key");
            }

            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var d = new BigInteger(privateKey, 16);
            var q = domain.G.Multiply(d);
            var publicKey = new ECPublicKeyParameters(q, domain);
            return Base58Encoding.Encode(publicKey.Q.GetEncoded());
        }
    }
}
