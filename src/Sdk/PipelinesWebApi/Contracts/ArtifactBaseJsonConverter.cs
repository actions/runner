using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.Actions.Pipelines.WebApi
{
    public abstract class ArtifactBaseJsonConverter<T> : VssSecureJsonConverter where T : class
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        // by returning false, the converter doesn't take part in writes
        // which means we use the default serialization logic
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        protected abstract T Create(Type objectType);

        protected abstract T Create(ArtifactType type);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
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

            // if objectType is one of our known types, we can ignore the type property
            T targetObject = Create(objectType);

            // read the data into a JObject so we can look at it
            var value = JObject.Load(reader);

            if (targetObject == null)
            {
                // use the Type property
                var typeProperty = contract.Properties.GetClosestMatchProperty("Type");
                if (typeProperty == null)
                {
                    // we don't know the type. just bail
                    return existingValue;
                }

                if (!value.TryGetValue(typeProperty.PropertyName, StringComparison.OrdinalIgnoreCase, out var typeValue))
                {
                    // a type property exists on the contract, but the JObject has no value for it
                    return existingValue;
                }

                var type = UnknownEnum.Parse<ArtifactType>(typeValue.ToString());
                targetObject = Create(type);
            }

            if (targetObject != null)
            {
                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, targetObject);
                }
            }

            return targetObject;
        }
    }
}
