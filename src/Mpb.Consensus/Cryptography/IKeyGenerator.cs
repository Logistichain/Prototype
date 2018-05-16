namespace Mpb.Consensus.Cryptography
{
    public interface IKeyGenerator
    {
        void GenerateKeys(out string publicKey, out string privateKey);
        string GeneratePublicKey(string privateKey);
    }
}