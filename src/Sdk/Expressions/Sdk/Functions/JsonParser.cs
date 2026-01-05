#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Text;
using System.Text.Json;
using GitHub.Actions.Expressions.Data;

namespace GitHub.Actions.Expressions.Sdk.Functions
{
    internal sealed class JsonParser
    {
        public static ExpressionData Parse(string json)
        {
            var reader = new Utf8JsonReader(
                Encoding.UTF8.GetBytes(json),
                new JsonReaderOptions{
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 100,
                });

            // EOF?
            if (!reader.Read())
            {
                throw new Exception("Expected at least one JSON token");
            }

            // Read
            var result = ReadRecursive(ref reader);

            // Not EOF?
            if (reader.Read())
            {
                throw new Exception($"Expected end of JSON but encountered '{reader.TokenType}'");
            }

            return result;
        }

        private static ExpressionData ReadRecursive(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    return ReadArray(ref reader);
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader);
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.False:
                    return new BooleanExpressionData(false);
                case JsonTokenType.True:
                    return new BooleanExpressionData(true);
                case JsonTokenType.Number:
                    return new NumberExpressionData(reader.GetDouble());
                case JsonTokenType.String:
                    return new StringExpressionData(reader.GetString());
                default:
                    throw new Exception($"Unexpected token type '{reader.TokenType}'");
            }
        }

        private static ArrayExpressionData ReadArray(ref Utf8JsonReader reader)
        {
            var result = new ArrayExpressionData();
            while (reader.Read())
            {
                // End array
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return result;
                }

                // Item
                result.Add(ReadRecursive(ref reader));
            }

            // EOF
            throw new Exception($"Unexpected end of JSON while reading array");
        }

        private static DictionaryExpressionData ReadObject(ref Utf8JsonReader reader)
        {
            var result = new DictionaryExpressionData();
            while (reader.Read())
            {
                var key = null as string;
                switch (reader.TokenType)
                {
                    // End object
                    case JsonTokenType.EndObject:
                        return result;

                    // Property name
                    case JsonTokenType.PropertyName:
                        key = reader.GetString();
                        break;

                    default:
                        throw new Exception($"Unexpected token type '{reader.TokenType}' while reading object");
                }

                // Value
                var value = null as ExpressionData;
                if (reader.Read())
                {
                    value = ReadRecursive(ref reader);
                }
                else
                {
                    throw new Exception("Unexpected end of JSON when reading object-pair value");
                }

                // Add
                if (!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }

            // EOF
            throw new Exception($"Unexpected end of JSON while reading object");
        }
    }
}
