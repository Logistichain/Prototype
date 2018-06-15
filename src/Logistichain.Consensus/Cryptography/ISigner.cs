namespace Logistichain.Consensus.Cryptography
{
    public interface ISigner
    {
        bool SignatureIsValid(string signature, string contents, string publicKey);
        string SignString(string contents, string privateKey);
    }
}