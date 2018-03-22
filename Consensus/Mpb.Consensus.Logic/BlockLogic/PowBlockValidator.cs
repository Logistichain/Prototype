using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Mpb.Consensus.Model;

namespace Mpb.Consensus.Logic.BlockLogic
{
    public class PowBlockValidator
    {
        internal bool BlockIsValid(Block b, BigDecimal currentTarget, byte[] blockHash)
        {
            var hashString = BitConverter.ToString(blockHash).Replace("-", "");
            BigDecimal hashValue = BigInteger.Parse(hashString, NumberStyles.HexNumber);

            // Hash value must be lower than the target and the first byte must be zero
            // because the first byte indidates if the hashValue is a positive or negative number,
            // negative numbers are not allowed.
            if (hashValue < currentTarget && hashString.StartsWith("0"))
            {
                return true;
            }

            return false;
        }
    }
}
