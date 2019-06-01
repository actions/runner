using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public enum VariableType
    {
        Inline = 0,
        Group = 1,
    }

    [JsonConverter(typeof(VariableJsonConverter))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVariable
    {
        VariableType Type { get; }
    }

    internal class VariableJsonConverter : VssSecureJsonConverter
    {
        public VariableJsonConverter()
        {
        }

        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
        }

        public override Boolean CanConvert(Type objectType)
        {
            return typeof(IVariable).IsAssignableFrom(objectType);
        }

        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var resultObj = JObject.Load(reader);
            var variableType = VariableType.Inline;
            if (resultObj.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out var rawValue))
            {
                if (rawValue.Type == JTokenType.Integer)
                {
                    variableType = (VariableType)(Int32)rawValue;
                }
                if (rawValue.Type == JTokenType.String)
                {
                    variableType = (VariableType)Enum.Parse(typeof(VariableType), (String)rawValue, true);
                }
            }
            else if (resultObj.TryGetValue("id", StringComparison.OrdinalIgnoreCase, out _) ||
                     resultObj.TryGetValue("groupType", StringComparison.OrdinalIgnoreCase, out _) ||
                     resultObj.TryGetValue("secretStore", StringComparison.OrdinalIgnoreCase, out _))
            {
                variableType = VariableType.Group;
            }

            IVariable result = null;
            switch (variableType)
            {
                case VariableType.Group:
                    result = new VariableGroupReference();
                    break;

                default:
                    result = new Variable();
                    break;
            }

            using (var objectReader = resultObj.CreateReader())
            {
                serializer.Populate(objectReader, result);
            }

            return result;
        }
    }
}
