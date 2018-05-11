using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Shared.Events
{
    /// <summary>
    /// Whenever a new block gets mined, this event will be triggered
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="eventHandler">The new block data</param>
    public delegate void BlockCreatedEventHandler(object sender, BlockCreatedEventArgs eventHandler);

    /// <summary>
    /// Triggered when a new transaction is created by ourselves,
    /// or when we receive a transaction from another network node.
    /// This event enables us to relay the transaction to other network nodes.
    /// Coinbase transactions aren't relayed.
    /// CAUTION: Only trigger this event when the transaction is valid!
    /// </summary>
    /// <param name="sender">The object that created the transaction</param>
    /// <param name="eventHandler">The new transaction</param>
    public delegate void TransactionReceivedEventHandler(object sender, TransactionReceivedEventArgs eventHandler);
}
