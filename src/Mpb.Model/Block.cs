using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Model
{
    public class Block
    {
        private BlockHeader _header;
        protected readonly IEnumerable<AbstractTransaction> _transactions;

        public BlockHeader Header => _header;
        public IEnumerable<AbstractTransaction> Transactions => _transactions;

        public Block(BlockHeader header, IEnumerable<AbstractTransaction> transactions)
        {
            _header = header;
            _transactions = transactions;
        }
    }
}
