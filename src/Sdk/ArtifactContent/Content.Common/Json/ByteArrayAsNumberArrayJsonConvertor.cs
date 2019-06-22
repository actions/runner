using System;
using System.Linq;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.Content.Common
{
    public sealed class ByteArrayAsNumberArrayJsonConvertor : VssSecureJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            return serializer.Deserialize<int[]>(reader).Select(x => (byte) x).ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            serializer.Serialize(writer, ((byte[])value).Select(x => (int)x).ToArray(), typeof(int[]));
        }
    }
}
