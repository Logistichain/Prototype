﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Exceptions
{
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