using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public class GenesisBlock : Block
    {
        public GenesisBlock(string networkIdentifier, uint transactionVersion)
            : base(BuildBlockHeader(networkIdentifier, transactionVersion), BuildTransactions())
        {
        }

        private static BlockHeader BuildBlockHeader(string networkIdentifier, uint transactionVersion)
        {
            // todo make this valid & signature
            var header = new BlockHeader(networkIdentifier, transactionVersion, "", 1, null);
            header.SetMerkleRoot("merkleroot");
            header.Finalize("genesis", "signature");
            return header;
        }

        private static IEnumerable<AbstractTransaction> BuildTransactions()
        {
            // No premine.
            return new List<AbstractTransaction>();
        }
    }
}
