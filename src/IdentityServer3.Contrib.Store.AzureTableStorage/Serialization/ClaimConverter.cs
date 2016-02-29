using System;
using System.Security.Claims;
using Newtonsoft.Json;

namespace IdentityServer3.Contrib.Store.AzureTableStorage.Serialization
{
    internal class ClaimConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (Claim)value;
            var target = new ClaimLite
            {
                Type = source.Type,
                Value = source.Value
            };
            serializer.Serialize(writer, target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ClaimLite>(reader);
            var target = new Claim(source.Type, source.Value);
            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Claim) == objectType;
        }
    }
}
