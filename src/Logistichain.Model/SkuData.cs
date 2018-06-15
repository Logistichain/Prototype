using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Model
{
    public class SkuData
    {
        private readonly string _skuId;
        private readonly string _eanCode;
        private readonly string _description;

        public SkuData(string skuId, string eanCode, string description)
        {
            _skuId = skuId;
            _eanCode = eanCode;
            _description = description;
        }

        public string Description => _description;

        public string EanCode => _eanCode;

        public string SkuId => _skuId;
    }
}
