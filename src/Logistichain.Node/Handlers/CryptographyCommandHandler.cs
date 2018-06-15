using Logistichain.DAL;
using Logistichain.Consensus.TransactionLogic;
using Logistichain.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Logistichain.Shared.Events;
using Logistichain.Consensus.Cryptography;

namespace Logistichain.Node.Handlers
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
