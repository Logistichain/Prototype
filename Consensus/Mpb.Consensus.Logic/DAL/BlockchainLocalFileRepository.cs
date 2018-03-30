using System;
using System.Collections.Generic;
using System.Text;
using Mpb.Consensus.Model;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using Mpb.Consensus.Logic.MiscLogic;

namespace Mpb.Consensus.Logic.DAL
{
    public class BlockchainLocalFileRepository : IBlockchainRepository
    {
        private string _blockchainFolderPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private Blockchain _trackingBlockchain;

        /// <summary>
        /// Serializes a Blockchain object to JSON and saves that to the given path.
        /// </summary>
        /// <param name="chain">The blockchain object that needs to be persisted</param>
        public void Update(Blockchain chain)
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
        public Blockchain GetByNetId(string netIdentifier)
        {
            if (_trackingBlockchain == null)
            {
                _trackingBlockchain = LoadBlockchainFromFileSystem(netIdentifier);
            }

            return _trackingBlockchain;
        }

        private Blockchain LoadBlockchainFromFileSystem(string netIdentifier)
        {
            var filePath = Path.Combine(_blockchainFolderPath, $"blockchain-{netIdentifier}.json");
            try
            {
                var blockchainFile = File.OpenRead(filePath);
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader stream = new StreamReader(blockchainFile))
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.Converters.Add(new BlockchainJsonConverter());
                    return JsonConvert.DeserializeObject<Blockchain>(stream.ReadToEnd(), settings);
                }
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                // File does not exist, return a new blockchain.
                var loadedBlockchain = new Blockchain(netIdentifier);
                return loadedBlockchain;
            }
        }
    }
}
