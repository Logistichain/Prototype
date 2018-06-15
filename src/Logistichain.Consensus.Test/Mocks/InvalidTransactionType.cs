using Logistichain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Consensus.Test.Mocks
{
    public class InvalidTransactionType : AbstractTransaction
    {
        public InvalidTransactionType(uint version, string action, string data, uint fee) : base(version, action, data, fee)
        {
        }
    }
}
