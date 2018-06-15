using Mpb.Model;
using System;

namespace Mpb.Shared.Events
{
    public class TransactionReceivedEventArgs
    {        
        public TransactionReceivedEventArgs(AbstractTransaction transaction)
        {
            Transaction = transaction;
        }

        public AbstractTransaction Transaction { get; }
    }
}