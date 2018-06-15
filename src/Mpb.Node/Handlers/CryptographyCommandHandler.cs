using Mpb.DAL;
using Mpb.Consensus.TransactionLogic;
using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Mpb.Shared.Events;
using Mpb.Consensus.Cryptography;

namespace Mpb.Node.Handlers
{
    internal class CryptographyCommandhandler
    {
        private readonly IKeyGenerator _keyGenerator;

        internal CryptographyCommandhandler(IKeyGenerator keyGenerator)
        {
            _keyGenerator = keyGenerator;
        }

        internal void HandleGenerateKeysCommand(out string publicKey, out string privateKey)
        {
            _keyGenerator.GenerateKeys(out publicKey, out privateKey);
        }
    }
}
