using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Model
{
    public class SkuData
    {
        private readonly string _skuId;
        private readonly string _eanCode;
        private readonly string _description;

        public SkuData(string SkuId, string EanCode, string description)
        {
            _skuId = SkuId;
            _eanCode = EanCode;
            _description = description;
        }

        public string Description => _description;

        public string EanCode => _eanCode;

        public string SkuId => _skuId;
    }
}
