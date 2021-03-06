﻿using Logistichain.Model;
using System;

namespace Logistichain.Shared.Events
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