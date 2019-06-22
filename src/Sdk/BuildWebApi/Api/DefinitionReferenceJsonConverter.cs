using System;
using System.Reflection;
using GitHub.Build.WebApi.Internals;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.Build.WebApi
{
    internal sealed class DefinitionReferenceJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(DefinitionReference).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
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

            Object newValue = null;

            if (objectType == typeof(BuildDefinition))
            {
                newValue = new BuildDefinition();
            }
            else if (objectType == typeof(BuildDefinition3_2))
            {
                newValue = new BuildDefinition3_2();
            }
            else if (objectType == typeof(BuildDefinitionReference))
            {
                newValue = new BuildDefinitionReference();
            }

            JObject value = JObject.Load(reader);

            if (newValue == null)
            {
                var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
                if (contract == null)
                {
                    return existingValue;
                }

                JsonProperty property = contract.Properties.GetClosestMatchProperty("Type");
                if (property == null)
                {
                    return existingValue;
                }

                JToken definitionTypeValue;
                DefinitionType definitionType = DefinitionType.Build;
                if (value.TryGetValue(property.PropertyName, out definitionTypeValue))
                {
                    if (definitionTypeValue.Type == JTokenType.Integer)
                    {
                        definitionType = (DefinitionType)(Int32)definitionTypeValue;
                    }
                    else if (definitionTypeValue.Type != JTokenType.String ||
                             !Enum.TryParse<DefinitionType>((String)definitionTypeValue, true, out definitionType))
                    {
                        definitionType = DefinitionType.Build;
                    }
                }

                switch (definitionType)
                {
                    case DefinitionType.Build:
                    default: // this is build2, after all
                        newValue = new BuildDefinition();
                        break;
                }
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
            // The virtual method returns false for CanWrite so this should never be invoked
            throw new NotSupportedException();
        }
    }
}
