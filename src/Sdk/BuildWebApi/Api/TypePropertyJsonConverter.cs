using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.Build.WebApi
{
    internal abstract class TypePropertyJsonConverter<TInstance> : VssSecureJsonConverter where TInstance : class
    {
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

            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
            {
                return existingValue;
            }

            var property = contract.Properties.GetClosestMatchProperty("Type");
            if (property == null)
            {
                return existingValue;
            }

            Int32 targetType;
            JToken targetTypeValue;
            var value = JObject.Load(reader);

            TInstance newValue = GetInstance(objectType);
            if (newValue == null)
            {
                if (!value.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out targetTypeValue))
                {
                    if (!TryInferType(value, out targetType))
                    {
                        return existingValue;
                    }
                }
                else
                {
                    if (targetTypeValue.Type != JTokenType.Integer)
                    {
                        return existingValue;
                    }
                    else
                    {
                        targetType = (Int32)targetTypeValue;
                    }
                }

                newValue = GetInstance(targetType);
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

        protected abstract TInstance GetInstance(Int32 targetType);

        protected virtual TInstance GetInstance(
            Type objectType)
        {
            return null;
        }

        protected virtual Boolean TryInferType(
            JObject value,
            out Int32 type)
        {
            type = 0;
            return false;
        }

        public override Boolean CanConvert(
            Type objectType)
        {
            return typeof(TInstance).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
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
