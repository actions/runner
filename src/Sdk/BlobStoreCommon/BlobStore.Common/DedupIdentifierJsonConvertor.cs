using System;
using System.Linq;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.BlobStore.Common
{
    public sealed class DedupIdentifierJsonConvertor : VssSecureJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsSubclassOf(typeof(DedupIdentifier));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return DedupIdentifier.Create(serializer.Deserialize<string>(reader));
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            serializer.Serialize(writer, ((DedupIdentifier)value).ValueString, typeof(string));
        }
    }
}
