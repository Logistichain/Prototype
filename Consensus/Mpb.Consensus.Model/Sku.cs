using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class Sku
    {
        private readonly Block _block;
        private readonly AbstractTransaction _transaction;
        private readonly SkuData _data;

        public Sku(Block block, AbstractTransaction transaction, SkuData data)
        {
            _block = block;
            _transaction = transaction;
            _data = data;
        }

        public SkuData Data => _data;

        public Block Block => _block;

        public AbstractTransaction Transaction => _transaction;
    }
}
