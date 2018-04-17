using Mpb.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpb.DAL.Converters
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
            List<BlockWithStateTransactions> stateTransactionBlocks = new List<BlockWithStateTransactions>();
            if (jBlocks != null && jBlocks.Type == JTokenType.Array)
            {
                // BlockWithStateTransactions is required, otherwise the json serializer
                // tries to instantiate an AbstractTransaction.
                // The list will be converted back to regular blocks later on.
                stateTransactionBlocks = jBlocks.ToObject<List<BlockWithStateTransactions>>(serializer);
            }
            var blocks = stateTransactionBlocks.OfType<Block>().ToList();
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
