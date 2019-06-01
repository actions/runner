using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal sealed class DemandJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(Demand).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            if (existingValue == null && reader.TokenType == JsonToken.String)
            {
                Demand demand;
                if (Demand.TryParse((String)reader.Value, out demand))
                {
                    existingValue = demand;
                }
            }

            return existingValue;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            if (value != null)
            {
                writer.WriteValue(value.ToString());
            }
        }
    }
}
