using Microsoft.Extensions.Logging;
using Mpb.Consensus.BlockLogic;
using Mpb.Consensus.Exceptions;
using Mpb.DAL;
using Mpb.Model;
using Mpb.Networking.Constants;
using Mpb.Networking.Model;
using Mpb.Networking.Model.MessagePayloads;
using Mpb.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mpb.Networking
{
    public class MessageHandler : AbstractMessageHandler, IMessageHandler
    {
        private readonly IDifficultyCalculator _difficultyCalculator;
        private readonly IBlockValidator _blockValidator;
        private readonly IBlockchainRepository _blockchainRepo;
        private readonly string _netId;

        public MessageHandler(INetworkManager manager, NetworkNodesPool nodePool, IDifficultyCalculator difficultyCalculator, IBlockValidator blockValidator, ILoggerFactory loggerFactory, IBlockchainRepository blockchainRepo, string netId)
            : base(manager, nodePool, loggerFactory)
        {
            _difficultyCalculator = difficultyCalculator;
            _blockValidator = blockValidator;
            _blockchainRepo = blockchainRepo;
            _netId = netId;
        }

        // todo Chain of Responsibility pattern, make XXMessageHandler class for each command type
        // and refactor abstractmessagehandler to a regular MessageHandlerHelper
        public async Task HandleMessage(NetworkNode node, Message msg)
        {
            try
            {
                if (msg.Command == NetworkCommand.CloseConn.ToString())
                {
                    await node.Disconnect();
                }
                else if (msg.Command == NetworkCommand.GetAddr.ToString())
                {
                    // Send our known peers
                    var addresses = new AddrPayload(_nodePool.GetAllRemoteListenEndpoints());
                    await SendMessageToNode(node, NetworkCommand.Addr, addresses);
                }
                else if (msg.Command == NetworkCommand.Addr.ToString())
                {
                    // Connect to all neighbors that the other node knows
                    var payload = (AddrPayload)msg.Payload;
                    foreach (IPEndPoint endpoint in payload.Endpoints)
                    {
                        await _networkManager.ConnectToPeer(endpoint);
                    }
                }
                else if (msg.Command == NetworkCommand.GetHeaders.ToString())
                {
                    // Send our headers
                    var payload = (GetHeadersPayload)msg.Payload;
                    try
                    {
                        var headers = GetBlocksFromHash(payload.HighestHeightHash, payload.StoppingHash, false).Select(b => b.Header);
                        var headersPayload = new HeadersPayload(headers);
                        await SendMessageToNode(node, NetworkCommand.Headers, headersPayload);
                    }
                    catch (KeyNotFoundException)
                    {
                        // Send empty payload
                        await SendMessageToNode(node, NetworkCommand.NotFound, null);
                    }
                }
                else if (msg.Command == NetworkCommand.GetBlocks.ToString())
                {
                    var payload = (GetBlocksPayload)msg.Payload;
                    var blocksPayload = new StateBlocksPayload();
                    if (payload.Headers.Count() > 0)
                    {
                        blocksPayload = new StateBlocksPayload(GetBlocksFromHash(payload.Headers.First(), payload.Headers.Last(), true));
                    }

                    await SendMessageToNode(node, NetworkCommand.Blocks, blocksPayload);
                }
                else if (msg.Command == NetworkCommand.Headers.ToString() && node.IsSyncingWithNode)
                {
                    node.SetSyncStatus(SyncStatus.InProgress);
                    var headersPayload = (HeadersPayload)msg.Payload;

                    if (headersPayload.Headers.Count() == 0)
                    {
                        _logger.LogInformation("Successfully synced with remote node.");
                        node.SetSyncStatus(SyncStatus.Succeeded);
                        return;
                    }

                    // Request these blocks
                    var getBlocksPayload = new GetBlocksPayload(headersPayload.Headers.Select(h => h.Hash));
                    await SendMessageToNode(node, NetworkCommand.GetBlocks, getBlocksPayload);
                }
                else if (msg.Command == NetworkCommand.NotFound.ToString() && node.IsSyncingWithNode)
                {
                    node.SetSyncStatus(SyncStatus.Failed); // Restart the syncing process with another node.
                }
                else if (msg.Command == NetworkCommand.Blocks.ToString() && node.IsSyncingWithNode)
                {
                    var blocksPayload = (StateBlocksPayload)msg.Payload;

                    // Todo rewrite this code to support multithreaded 'Blocks' messages. Combine all gathered blocks
                    // until the process has completed and all blocks are downloaded. Then, grab a block that points to the
                    // end of our chain and add it to our chain. Repeat that process until all blocks have been added.

                    var blockchain = _blockchainRepo.GetChainByNetId(_netId);
                    var blocksProcessed = 0;

                    while (blocksPayload.Blocks.Where(b => b.Header.PreviousHash == blockchain.Blocks.Last().Header.Hash).Any())
                    {
                        var blockToProcess = blocksPayload.Blocks.Where(b => b.Header.PreviousHash == blockchain.Blocks.Last().Header.Hash).First();
                        var difficulty = _difficultyCalculator.CalculateDifficulty(blockchain, blockchain.CurrentHeight, 1, 3, 5); // todo use CalculateCurrentDifficulty when testing is done
                        if (difficulty < 1) { difficulty = 1; }
                        var currentTarget = BlockchainConstants.MaximumTarget / difficulty; // todo do something with these 3 lines. They come from the miner.
                        _blockValidator.ValidateBlock(blockToProcess, currentTarget, blockchain, false, true); // Rethrow when we have a Block- / TransactionRejectedException. We don't want to keep a connection with bad nodes.
                        blocksProcessed++;
                    }

                    _logger.LogDebug("Downloaded and added {0} new blocks from remote node", blocksProcessed);
                    _logger.LogDebug("Current height: {0}", blockchain.CurrentHeight);

                    if (blocksProcessed != blocksPayload.Blocks.Count())
                    {
                        _logger.LogError("Added {0} new blocks from remote node, but expected {1}. Sync failed.", blocksProcessed, blocksPayload.Blocks.Count());
                        node.SetSyncStatus(SyncStatus.Failed);
                        return;
                    }

                    _blockchainRepo.Update(blockchain);

                    // Block batch processed. Keep on ask for more headers.
                    var getHeadersPayload = new GetHeadersPayload(blockchain.Blocks.Last().Header.Hash);
                    await SendMessageToNode(node, NetworkCommand.GetHeaders, getHeadersPayload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An {0} occurred during the process of handling a {1} message: {2}. Node will be disconnected.", ex.GetType().Name, msg.Command.ToString(), ex.Message);
                node?.Disconnect();
            }
        }

        private IEnumerable<Block> GetBlocksFromHash(string beginHash, string stopHash, bool includeBeginBlock)
        {
            var blocks = new List<Block>();
            bool stopSearching = false;
            var previousBlock = _blockchainRepo.GetBlockByHash(beginHash, _netId);
            var i = 0;

            if (includeBeginBlock)
            {
                blocks.Add(previousBlock);
            }

            while (i < NetworkConstants.MaxHeadersInMessage && !stopSearching)
            {
                // Stale blocks / side chains are not supported here
                try
                {
                    previousBlock = _blockchainRepo.GetBlockByPreviousHash(previousBlock.Header.Hash, _netId);
                    blocks.Add(previousBlock);

                    if (previousBlock.Header.Hash == stopHash)
                    {
                        stopSearching = true;
                    }
                }
                catch (KeyNotFoundException)
                {
                    stopSearching = true; // No more blocks found
                }
                i++;
            }

            return blocks;
        }
    }
}
