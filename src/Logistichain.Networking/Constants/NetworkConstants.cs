﻿using System;
using System.Globalization;
using System.Numerics;

namespace Logistichain.Networking.Constants
{
    public class NetworkConstants
    {
        /// <summary>
        /// When there are no expected messages to be received,
        /// use this timeout. After this timeout as passed, assume
        /// the peer isn't connected anymore.
        /// </summary>
        public const int IdleTimeoutSeconds = 1800;

        /// <summary>
        /// Use this timeout when expecting a message,
        /// like in the initial 'version' handshake.
        /// </summary>
        public const int ExpectMsgTimeoutSeconds = 60;

        /// <summary>
        /// Maximum amount of peers to connect to.
        /// </summary>
        public const int MaxConcurrentConnections = 30;

        /// <summary>
        /// Maximum amount of headers to send in one message.
        /// </summary>
        public const int MaxHeadersInMessage = 5000; // 5000 in production, 5 for message testing

        /// <summary>
        /// Maximum amount of blocks to send in one message.
        /// </summary>
        public const int MaxBlocksInMessage = 3000; // 3000 in production, 3 for message testing

        /// <summary>
        /// Every host listens on this port by default.
        /// Can be overridden without any issues.
        /// </summary>
        public const ushort DefaultListeningPort = 10101;
    }
}
