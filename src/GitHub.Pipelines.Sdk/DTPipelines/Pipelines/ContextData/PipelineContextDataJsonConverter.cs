using System;
using System.Reflection;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData
{
    /// <summary>
    /// JSON serializer for ContextData objects
    /// </summary>
    internal sealed class PipelineContextDataJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanWrite
        {
            get
            {
                return true;
            }
        }

        public override Boolean CanConvert(Type objectType)
        {
            return typeof(PipelineContextData).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return new StringContextData(reader.Value.ToString());
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            Int32? type = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("t", StringComparison.OrdinalIgnoreCase, out JToken typeValue))
            {
                type = PipelineContextDataType.String;
            }
            else if (typeValue.Type == JTokenType.Integer)
            {
                type = (Int32)typeValue;
            }
            else
            {
                return existingValue;
            }

            Object newValue = null;
            switch (type)
            {
                case PipelineContextDataType.String:
                    newValue = new StringContextData(null);
                    break;

                case PipelineContextDataType.Array:
                    newValue = new ArrayContextData();
                    break;

                case PipelineContextDataType.Dictionary:
                    newValue = new DictionaryContextData();
                    break;

                default:
                    throw new NotSupportedException($"Unexpected {nameof(PipelineContextDataType)} '{type}'");
            }

            if (value != null)
            {
                using (JsonReader objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, newValue);
                }
            }

            return newValue;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            if (Object.ReferenceEquals(value, null))
            {
                writer.WriteNull();
            }
            else if (value is StringContextData stringData)
            {
                writer.WriteValue(stringData.Value);
            }
            else if (value is ArrayContextData arrayData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("t");
                writer.WriteValue(PipelineContextDataType.Array);
                if (arrayData.Count > 0)
                {
                    writer.WritePropertyName("a");
                    writer.WriteStartArray();
                    foreach (var item in arrayData)
                    {
                        serializer.Serialize(writer, item);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            else if (value is DictionaryContextData dictionaryData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("t");
                writer.WriteValue(PipelineContextDataType.Dictionary);
                if (dictionaryData.Count > 0)
                {
                    writer.WritePropertyName("d");
                    writer.WriteStartArray();
                    foreach (var pair in dictionaryData)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("k");
                        writer.WriteValue(pair.Key);
                        writer.WritePropertyName("v");
                        serializer.Serialize(writer, pair.Value);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            else
            {
                throw new NotSupportedException($"Unexpected type '{value.GetType().Name}'");
            }
        }
    }
}