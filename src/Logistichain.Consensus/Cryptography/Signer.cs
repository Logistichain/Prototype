using Logistichain.Shared.Extensions;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Logistichain.Consensus.Cryptography
{
    // © Sander Gerz https://blog.todotnet.com/2018/02/public-private-keys-and-signing/
    public class Signer : ISigner
    {
        public string SignString(string contents, string privateKey)
        {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var keyParameters = new ECPrivateKeyParameters(new BigInteger(privateKey, 16), domain);
            Org.BouncyCastle.Crypto.ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, keyParameters);
            signer.BlockUpdate(Encoding.ASCII.GetBytes(contents), 0, contents.Length);
            var signature = signer.GenerateSignature();
            return Base58Encoding.Encode(signature);
        }

        public bool SignatureIsValid(string signature, string contents, string publicKey)
        {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var publicKeyBytes = Base58Encoding.Decode(publicKey);
            var q = curve.Curve.DecodePoint(publicKeyBytes);
            var keyParameters = new ECPublicKeyParameters(q, domain);
            Org.BouncyCastle.Crypto.ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(false, keyParameters);
            signer.BlockUpdate(Encoding.ASCII.GetBytes(contents), 0, contents.Length);
            var signatureBytes = Base58Encoding.Decode(signature);
            return signer.VerifySignature(signatureBytes);
        }
    }
}
