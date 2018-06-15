using Mpb.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Exceptions
{
    public class TransactionRejectedException : Exception
    {
        /// <summary>
        /// Throw this exception when the given transaction doesn't pass validation.
        /// </summary>
        public TransactionRejectedException()
        {
        }

        public TransactionRejectedException(string message) : base(message)
        {
        }

        public TransactionRejectedException(string message, AbstractTransaction t) : base(message)
        {
            Transaction = t;
        }

        public AbstractTransaction Transaction { get; }
    }
}
