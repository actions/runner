using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    internal sealed class ValidationItemJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(ValidationItem).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            if (objectType == typeof(ExpressionValidationItem))
            {
                newValue = new ExpressionValidationItem();
            }
            else if (objectType == typeof(InputValidationItem))
            {
                newValue = new InputValidationItem();
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

                JToken itemTypeValue;
                String itemType = InputValidationTypes.Expression;
                if (value.TryGetValue(property.PropertyName, out itemTypeValue)
                    && itemTypeValue.Type == JTokenType.String)
                {
                    itemType = (String)itemTypeValue;
                }

                switch (itemType)
                {
                    case InputValidationTypes.Expression:
                        newValue = new ExpressionValidationItem();
                        break;
                    case InputValidationTypes.Input:
                        newValue = new InputValidationItem();
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
