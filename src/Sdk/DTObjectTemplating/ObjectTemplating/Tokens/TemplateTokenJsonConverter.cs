using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// JSON serializer for TemplateToken objects
    /// </summary>
    internal sealed class TemplateTokenJsonConverter : VssSecureJsonConverter
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
            return typeof(TemplateToken).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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
                    return new StringToken(null, null, null, reader.Value.ToString());
                case JsonToken.Boolean:
                    return new BooleanToken(null, null, null, (Boolean)reader.Value);
                case JsonToken.Float:
                    return new NumberToken(null, null, null, (Double)reader.Value);
                case JsonToken.Integer:
                    return new NumberToken(null, null, null, (Double)(Int64)reader.Value);
                case JsonToken.Null:
                    return new NullToken(null, null, null);
                case JsonToken.StartObject:
                    break;
                default:
                    return null;
            }

            Int32? type = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out JToken typeValue))
            {
                type = TokenType.String;
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
                case TokenType.Null:
                    newValue = new NullToken(null, null, null);
                    break;

                case TokenType.Boolean:
                    newValue = new BooleanToken(null, null, null, default(Boolean));
                    break;

                case TokenType.Number:
                    newValue = new NumberToken(null, null, null, default(Double));
                    break;

                case TokenType.String:
                    newValue = new StringToken(null, null, null, null);
                    break;

                case TokenType.BasicExpression:
                    newValue = new BasicExpressionToken(null, null, null, null);
                    break;

                case TokenType.InsertExpression:
                    newValue = new InsertExpressionToken(null, null, null);
                    break;

                case TokenType.Sequence:
                    newValue = new SequenceToken(null, null, null);
                    break;

                case TokenType.Mapping:
                    newValue = new MappingToken(null, null, null);
                    break;
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
            if (value is TemplateToken token)
            {
                switch (token.Type)
                {
                    case TokenType.Null:
                        if (token.FileId == null && token.Line == null && token.Column == null)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("type");
                            writer.WriteValue(token.Type);
                            if (token.FileId != null)
                            {
                                writer.WritePropertyName("file");
                                writer.WriteValue(token.FileId);
                            }
                            if (token.Line != null)
                            {
                                writer.WritePropertyName("line");
                                writer.WriteValue(token.Line);
                            }
                            if (token.Column != null)
                            {
                                writer.WritePropertyName("col");
                                writer.WriteValue(token.Column);
                            }
                            writer.WriteEndObject();
                        }
                        return;

                    case TokenType.Boolean:
                        var booleanToken = token as BooleanToken;
                        if (token.FileId == null && token.Line == null && token.Column == null)
                        {
                            writer.WriteValue(booleanToken.Value);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("type");
                            writer.WriteValue(token.Type);
                            if (token.FileId != null)
                            {
                                writer.WritePropertyName("file");
                                writer.WriteValue(token.FileId);
                            }
                            if (token.Line != null)
                            {
                                writer.WritePropertyName("line");
                                writer.WriteValue(token.Line);
                            }
                            if (token.Column != null)
                            {
                                writer.WritePropertyName("col");
                                writer.WriteValue(token.Column);
                            }
                            writer.WritePropertyName("bool");
                            writer.WriteValue(booleanToken.Value);
                            writer.WriteEndObject();
                        }
                        return;

                    case TokenType.Number:
                        var numberToken = token as NumberToken;
                        if (token.FileId == null && token.Line == null && token.Column == null)
                        {
                            writer.WriteValue(numberToken.Value);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("type");
                            writer.WriteValue(token.Type);
                            if (token.FileId != null)
                            {
                                writer.WritePropertyName("file");
                                writer.WriteValue(token.FileId);
                            }
                            if (token.Line != null)
                            {
                                writer.WritePropertyName("line");
                                writer.WriteValue(token.Line);
                            }
                            if (token.Column != null)
                            {
                                writer.WritePropertyName("col");
                                writer.WriteValue(token.Column);
                            }
                            writer.WritePropertyName("num");
                            writer.WriteValue(numberToken.Value);
                            writer.WriteEndObject();
                        }
                        return;

                    case TokenType.String:
                        var stringToken = token as StringToken;
                        if (token.FileId == null && token.Line == null && token.Column == null)
                        {
                            writer.WriteValue(stringToken.Value);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("type");
                            writer.WriteValue(token.Type);
                            if (token.FileId != null)
                            {
                                writer.WritePropertyName("file");
                                writer.WriteValue(token.FileId);
                            }
                            if (token.Line != null)
                            {
                                writer.WritePropertyName("line");
                                writer.WriteValue(token.Line);
                            }
                            if (token.Column != null)
                            {
                                writer.WritePropertyName("col");
                                writer.WriteValue(token.Column);
                            }
                            writer.WritePropertyName("lit");
                            writer.WriteValue(stringToken.Value);
                            writer.WriteEndObject();
                        }
                        return;

                    case TokenType.BasicExpression:
                        var basicExpressionToken = token as BasicExpressionToken;
                        writer.WriteStartObject();
                        writer.WritePropertyName("type");
                        writer.WriteValue(token.Type);
                        if (token.FileId != null)
                        {
                            writer.WritePropertyName("file");
                            writer.WriteValue(token.FileId);
                        }
                        if (token.Line != null)
                        {
                            writer.WritePropertyName("line");
                            writer.WriteValue(token.Line);
                        }
                        if (token.Column != null)
                        {
                            writer.WritePropertyName("col");
                            writer.WriteValue(token.Column);
                        }
                        if (!String.IsNullOrEmpty(basicExpressionToken.Expression))
                        {
                            writer.WritePropertyName("expr");
                            writer.WriteValue(basicExpressionToken.Expression);
                        }
                        writer.WriteEndObject();
                        return;

                    case TokenType.InsertExpression:
                        var insertExpressionToken = token as InsertExpressionToken;
                        writer.WriteStartObject();
                        writer.WritePropertyName("type");
                        writer.WriteValue(token.Type);
                        if (token.FileId != null)
                        {
                            writer.WritePropertyName("file");
                            writer.WriteValue(token.FileId);
                        }
                        if (token.Line != null)
                        {
                            writer.WritePropertyName("line");
                            writer.WriteValue(token.Line);
                        }
                        if (token.Column != null)
                        {
                            writer.WritePropertyName("col");
                            writer.WriteValue(token.Column);
                        }
                        writer.WritePropertyName("directive");
                        writer.WriteValue(insertExpressionToken.Directive);
                        writer.WriteEndObject();
                        return;

                    case TokenType.Sequence:
                        var sequenceToken = token as SequenceToken;
                        writer.WriteStartObject();
                        writer.WritePropertyName("type");
                        writer.WriteValue(token.Type);
                        if (token.FileId != null)
                        {
                            writer.WritePropertyName("file");
                            writer.WriteValue(token.FileId);
                        }
                        if (token.Line != null)
                        {
                            writer.WritePropertyName("line");
                            writer.WriteValue(token.Line);
                        }
                        if (token.Column != null)
                        {
                            writer.WritePropertyName("col");
                            writer.WriteValue(token.Column);
                        }
                        if (sequenceToken.Count > 0)
                        {
                            writer.WritePropertyName("seq");
                            writer.WriteStartArray();
                            foreach (var item in sequenceToken)
                            {
                                serializer.Serialize(writer, item);
                            }
                            writer.WriteEndArray();
                        }
                        writer.WriteEndObject();
                        return;

                    case TokenType.Mapping:
                        var mappingToken = token as MappingToken;
                        writer.WriteStartObject();
                        writer.WritePropertyName("type");
                        writer.WriteValue(token.Type);
                        if (token.FileId != null)
                        {
                            writer.WritePropertyName("file");
                            writer.WriteValue(token.FileId);
                        }
                        if (token.Line != null)
                        {
                            writer.WritePropertyName("line");
                            writer.WriteValue(token.Line);
                        }
                        if (token.Column != null)
                        {
                            writer.WritePropertyName("col");
                            writer.WriteValue(token.Column);
                        }
                        if (mappingToken.Count > 0)
                        {
                            writer.WritePropertyName("map");
                            writer.WriteStartArray();
                            foreach (var item in mappingToken)
                            {
                                serializer.Serialize(writer, item);
                            }
                            writer.WriteEndArray();
                        }
                        writer.WriteEndObject();
                        return;
                }
            }

            throw new NotSupportedException($"Unexpected type '{value?.GetType().FullName}' when serializing template token");
        }
    }
}
