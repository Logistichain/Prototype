using System;
using System.Collections.Generic;
using System.Text;

namespace Logistichain.Consensus.Exceptions
{
    /// <summary>
    /// Throw this exception when there are poblems with calculating the (new) difficulty/target.
    /// </summary>
    public class DifficultyCalculationException : Exception
    {
        public DifficultyCalculationException()
        {
        }

        public DifficultyCalculationException(string message) : base(message)
        {
        }
    }
}
