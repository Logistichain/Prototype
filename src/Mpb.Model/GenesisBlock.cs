using Mpb.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public class GenesisBlock : Block
    {
        public GenesisBlock() : base(BuildBlockHeader(), BuildTransactions())
        {
        }

        private static BlockHeader BuildBlockHeader()
        {
            // todo make this valid & signature
            var header = new BlockHeader(BlockchainConstants.DefaultNetworkIdentifier, BlockchainConstants.TransactionVersion, "", 1, null);
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
