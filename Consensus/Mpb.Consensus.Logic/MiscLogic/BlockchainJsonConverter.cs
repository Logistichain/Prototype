using Mpb.Consensus.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mpb.Consensus.Logic.MiscLogic
{
    /// <summary>
    /// This JsonConverted was created to prevent JSON-specific annotations or a parameterless constructor for the Blockchain class.
    /// </summary>
    public class BlockchainJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Blockchain));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string netId = (string)jo["NetIdentifier"];
            JToken jBlocks = jo["Blocks"];
            List<Block> blocks = new List<Block>();
            if (jBlocks != null && jBlocks.Type == JTokenType.Array)
            {
                blocks = jBlocks.ToObject<List<Block>>(serializer);
            }
            Blockchain result = new Blockchain(blocks, netId);
            return result;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
