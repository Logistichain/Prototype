using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Shared.Events
{
    public delegate void ValidatedBlockCreatedEventHandler(object sender, BlockCreatedEventArgs eventHandler);

    public delegate bool UnvalidatedBlockCreatedEventHandler(object sender, BlockCreatedEventArgs eventHandler);

    public delegate void ValidTransactionReceivedEventHandler(object sender, TransactionReceivedEventArgs eventHandler);

    public delegate bool UnvalidatedTransactionReceivedEventHandler(object sender, TransactionReceivedEventArgs eventHandler);
    
    public class EventPublisher
    {
        private static volatile EventPublisher _instance;
        private static object _threadLock = new Object();

        /// <summary>
        /// Whenever a new block gets mined, this event will be triggered
        /// </summary>
        /// <param name="sender">The object that triggered the event</param>
        /// <param name="eventHandler">The new block data</param>
        public event ValidatedBlockCreatedEventHandler OnValidatedBlockCreated;

        /// <summary>
        /// When we receive a new block from a node, this event gets triggered.
        /// The block will be validated and added to the chain, then 
        /// ValidatedBlockCreatedEventHandler triggers.
        /// </summary>
        /// <param name="sender">The object that triggered the event</param>
        /// <param name="eventHandler">The new block data</param>
        /// <returns>Whether the block is valid or not.</returns>
        public event UnvalidatedBlockCreatedEventHandler OnUnvalidatedBlockCreated;

        /// <summary>
        /// Triggered when a new transaction is verified and added to the txpool.
        /// This event enables us to relay the transaction to other network nodes.
        /// Coinbase transactions aren't relayed.
        /// CAUTION: Only trigger this event when the transaction is valid!
        /// </summary>
        /// <param name="sender">The object that created the transaction</param>
        /// <param name="eventHandler">The valid transaction</param>
        public event ValidTransactionReceivedEventHandler OnValidTransactionReceived;

        /// <summary>
        /// Triggered when an external system submits a new transaction.
        /// The transaction will be verified and added to the txpool before relaying.
        /// </summary>
        /// <param name="sender">The object that submitted the new transaction</param>
        /// <param name="eventHandler">The new transaction</param>
        /// <returns>Whether the transaction is valid or not.</returns>
        public event UnvalidatedTransactionReceivedEventHandler OnUnvalidatedTransactionReceived;

        private EventPublisher() { }

        /// <summary>
        /// Gets the singleton instance for this object.
        /// </summary>
        /// <returns></returns>
        public static EventPublisher GetInstance()
        {
            if (_instance == null)
            {
                lock (_threadLock)
                {
                    if (_instance == null)
                        _instance = new EventPublisher();
                }
            }

            return _instance;
        }

        public void PublishValidatedBlockCreated(object sender, BlockCreatedEventArgs ev)
        {
            OnValidatedBlockCreated?.Invoke(sender, ev);
        }

        /// <summary>
        /// This event is typically triggered when new blocks arrive from network nodes.
        /// The event will be picked up by the miner and tries to validate and add the block.
        /// Then the ValidatedBlockCreated event is triggered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        public bool? PublishUnvalidatedBlockCreated(object sender, BlockCreatedEventArgs ev)
        {
            return OnUnvalidatedBlockCreated?.Invoke(sender, ev);
        }

        public void PublishValidTransactionReceived(object sender, TransactionReceivedEventArgs ev)
        {
            OnValidTransactionReceived?.Invoke(sender, ev);
        }

        /// <summary>
        /// This event is typically triggered by other nodes publishing transactions.
        /// The event will be picked up by the txpool and tries to add it. When that's
        /// successful, OnValidTransactionReceived is triggered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        public bool? PublishUnvalidatedTransactionReceived(object sender, TransactionReceivedEventArgs ev)
        {
            return OnUnvalidatedTransactionReceived?.Invoke(sender, ev);
        }
    }
}
