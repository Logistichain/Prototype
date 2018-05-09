using System;
using System.Collections.Generic;
using System.Text;
using Mpb.Model;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using Mpb.DAL.Converters;

namespace Mpb.DAL
{
    public class BlockchainLocalFileRepository : IBlockchainRepository
    {
        private string _blockchainFolderPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private Blockchain _trackingBlockchain;
        private object fileLock = new object();

        /// <summary>
        /// Serializes a Blockchain object to JSON and saves that to the given path.
        /// </summary>
        /// <param name="chain">The blockchain object that needs to be persisted</param>
        public void Update(Blockchain chain)
        {
            lock (fileLock)
            {
                var filePath = Path.Combine(_blockchainFolderPath, $"blockchain-{chain.NetIdentifier}.json");
                var blockchainFile = File.Create(filePath);
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter stream = new StreamWriter(blockchainFile))
                using (JsonWriter writer = new JsonTextWriter(stream))
                {
                    serializer.Serialize(writer, chain);
                }
            }
        }

        /// <summary>
        /// REMOVE the blockchain file and start all over again.
        /// </summary>
        /// <param name="netIdentifier">The blockchain that will be removed</param>
        public void Delete(string netIdentifier)
        {
            var filePath = Path.Combine(_blockchainFolderPath, $"blockchain-{netIdentifier}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _trackingBlockchain = null;
            }
        }

        /// <summary>
        /// Returns the entire blockchain that is locally stored.
        /// If the blockchain file does not exist, a new blockchain object is returned.
        /// </summary>
        /// <returns>The deserialized blockchain object</returns>
        public Blockchain GetChainByNetId(string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                _trackingBlockchain = LoadBlockchainFromFileSystem(netIdentifier);
            }

            return _trackingBlockchain;
        }

        public Block GetBlockByHash(string blockHash, string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                GetChainByNetId(netIdentifier);
            }

            var searchQuery = _trackingBlockchain.Blocks.Where(tx => tx.Header.Hash.ToUpper() == blockHash.ToUpper());
            if (searchQuery.Count() > 0)
            {
                return searchQuery.First();
            }

            throw new KeyNotFoundException("Block not found in blockchain");
        }

        public Block GetBlockByPreviousHash(string previousBlockHash, string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                GetChainByNetId(netIdentifier);
            }

            var searchQuery = _trackingBlockchain.Blocks.Where(b => b.Header.PreviousHash != null && b.Header.PreviousHash.ToUpper() == previousBlockHash.ToUpper());
            if (searchQuery.Count() > 0)
            {
                return searchQuery.First();
            }

            throw new KeyNotFoundException("Previous hash not found in blockchain");
        }

        /// <summary>
        /// Returns a block which contains the transactionHash.
        /// Throws KeyNotFoundException when no block was found.
        /// </summary>
        /// <param name="transactionHash">The hash of the transaction</param>
        /// <param name="netIdentifier">The network identifier</param>
        /// <returns>The block that contains the transaction</returns>
        public Block GetBlockByTransactionHash(string transactionHash, string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                GetChainByNetId(netIdentifier);
            }

            foreach(var block in _trackingBlockchain.Blocks)
            {
                if (block.Transactions.Where(tx => tx.Hash == transactionHash).Count() > 0)
                {
                    return block;
                }
            }

            throw new KeyNotFoundException("Transaction not found in blockchain");
        }

        /// <summary>
        /// Get the height for a block in the given blockchain network.
        /// Throws KeyNotFoundException when no block was found.
        /// </summary>
        /// <param name="hash">The block hash</param>
        /// <param name="netIdentifier">The network identifier</param>
        /// <returns>The height for the given block hash</returns>
        public int GetHeightForBlock(string hash, string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                GetChainByNetId(netIdentifier);
            }
            
            int height = _trackingBlockchain.GetHeightForBlock(hash);

            if (height == -1)
            {
                throw new KeyNotFoundException("No block found with the given hash");
            }

            return height;
        }

        private Blockchain LoadBlockchainFromFileSystem(string netIdentifier)
        {
            var filePath = Path.Combine(_blockchainFolderPath, $"blockchain-{netIdentifier}.json");
            try
            {
                lock (fileLock)
                {
                    var blockchainFile = File.OpenRead(filePath);
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader stream = new StreamReader(blockchainFile))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.Converters.Add(new BlockHeaderJsonConverter());
                        settings.Converters.Add(new BlockchainJsonConverter());
                        settings.Converters.Add(new StateTransactionJsonConverter());
                        return JsonConvert.DeserializeObject<Blockchain>(stream.ReadToEnd(), settings);
                    }
                }
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                // File does not exist, return a new blockchain.
                var loadedBlockchain = new Blockchain(new List<Block>() { new GenesisBlock() }, netIdentifier);
                return loadedBlockchain;
            }
        }
    }
}
