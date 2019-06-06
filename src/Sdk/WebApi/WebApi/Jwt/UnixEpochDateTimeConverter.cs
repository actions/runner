using System;
using GitHub.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Services.WebApi.Jwt
{
    class UnixEpochDateTimeConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            long unixVal = reader.Value is string ? long.Parse((string) reader.Value) : (long)reader.Value;

            return unixVal.FromUnixEpochTime();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            long unixVal = ((DateTime)value).ToUnixEpochTime();

            writer.WriteValue(unixVal);
        }
    }
}
