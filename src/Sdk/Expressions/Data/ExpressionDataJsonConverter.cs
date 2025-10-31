#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Data
{
    /// <summary>
    /// JSON serializer for ExpressionData objects
    /// </summary>
    internal sealed class ExpressionDataJsonConverter : JsonConverter
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
            return typeof(ExpressionData).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return new StringExpressionData(reader.Value.ToString());

                case JsonToken.Boolean:
                    return new BooleanExpressionData((Boolean)reader.Value);

                case JsonToken.Float:
                    return new NumberExpressionData((Double)reader.Value);

                case JsonToken.Integer:
                    return new NumberExpressionData((Double)(Int64)reader.Value);

                case JsonToken.StartObject:
                    break;

                default:
                    return null;
            }

            Int32? type = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("t", StringComparison.OrdinalIgnoreCase, out JToken typeValue))
            {
                type = ExpressionDataType.String;
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
                case ExpressionDataType.String:
                    newValue = new StringExpressionData(null);
                    break;

                case ExpressionDataType.Array:
                    newValue = new ArrayExpressionData();
                    break;

                case ExpressionDataType.Dictionary:
                    newValue = new DictionaryExpressionData();
                    break;

                case ExpressionDataType.Boolean:
                    newValue = new BooleanExpressionData(false);
                    break;

                case ExpressionDataType.Number:
                    newValue = new NumberExpressionData(0);
                    break;

                case ExpressionDataType.CaseSensitiveDictionary:
                    newValue = new CaseSensitiveDictionaryExpressionData();
                    break;

                default:
                    throw new NotSupportedException($"Unexpected {nameof(ExpressionDataType)} '{type}'");
            }

            if (value != null)
            {
                using JsonReader objectReader = value.CreateReader();
                serializer.Populate(objectReader, newValue);
            }

            return newValue;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            if (Object.ReferenceEquals(value, null))
            {
                writer.WriteNull();
            }
            else if (value is StringExpressionData stringData)
            {
                writer.WriteValue(stringData.Value);
            }
            else if (value is BooleanExpressionData boolData)
            {
                writer.WriteValue(boolData.Value);
            }
            else if (value is NumberExpressionData numberData)
            {
                writer.WriteValue(numberData.Value);
            }
            else if (value is ArrayExpressionData arrayData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("t");
                writer.WriteValue(ExpressionDataType.Array);
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
            else if (value is DictionaryExpressionData dictionaryData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("t");
                writer.WriteValue(ExpressionDataType.Dictionary);
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
            else if (value is CaseSensitiveDictionaryExpressionData caseSensitiveDictionaryData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("t");
                writer.WriteValue(ExpressionDataType.CaseSensitiveDictionary);
                if (caseSensitiveDictionaryData.Count > 0)
                {
                    writer.WritePropertyName("d");
                    writer.WriteStartArray();
                    foreach (var pair in caseSensitiveDictionaryData)
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