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
                return false;
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
            if (reader.TokenType != JsonToken.StartObject)
            {
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
            throw new NotSupportedException();
        }
    }
}
