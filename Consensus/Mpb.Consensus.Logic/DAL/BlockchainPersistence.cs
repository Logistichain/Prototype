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
    public class BlockchainPersistence
    {
        private string blockchainFolderPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Serializes a Blockchain object to JSON and saves that to the given path.
        /// </summary>
        /// <param name="chain">The blockchain object that needs to be persisted</param>
        public void SaveBlockchain(Blockchain chain)
        {
            var filePath = Path.Combine(blockchainFolderPath, $"blockchain-{chain.NetIdentifier}.json");
            var blockchainFile = File.Create(filePath);
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter stream = new StreamWriter(blockchainFile))
            using (JsonWriter writer = new JsonTextWriter(stream))
            {
                serializer.Serialize(writer, chain);
            }
        }

        /// <summary>
        /// Returns the entire blockchain that is locally stored.
        /// If the blockchain file does not exist, a new blockchain object is returned.
        /// </summary>
        /// <returns>The deserialized blockchain object</returns>
        public Blockchain FindLocalBlockchain(string netIdentifier)
        {
            var filePath = Path.Combine(blockchainFolderPath, $"blockchain-{netIdentifier}.json");
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
