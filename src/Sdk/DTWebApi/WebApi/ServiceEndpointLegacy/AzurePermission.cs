using System;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [JsonConverter(typeof(AzurePermissionJsonConverter))]
    [KnownType(typeof(AzureKeyVaultPermission))]
    [DataContract]
    public abstract class AzurePermission
    {
        [DataMember]
        public String ResourceProvider { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public Boolean Provisioned { get; set; }

        internal AzurePermission(String resourceProvider)
        {
            this.ResourceProvider = resourceProvider;
        }
    }

    internal sealed class AzurePermissionJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanRead
        {
            get
            {
                return true;
            }
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
            return typeof(AzurePermission).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                return existingValue;
            }

            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
            {
                return existingValue;
            }

            JsonProperty resourceProviderProperty = contract.Properties.GetClosestMatchProperty("ResourceProvider");
            if (resourceProviderProperty == null)
            {
                return existingValue;
            }

            JToken itemTypeValue;
            JObject value = JObject.Load(reader);

            if (!value.TryGetValue(resourceProviderProperty.PropertyName, StringComparison.OrdinalIgnoreCase, out itemTypeValue))
            {
                return existingValue;
            }

            if (itemTypeValue.Type != JTokenType.String)
            {
                throw new NotSupportedException("ResourceProvider property is mandatory for azure permission");
            }

            string resourceProvider = (string)itemTypeValue;
            AzurePermission returnValue = null;
            switch (resourceProvider)
            {
                case AzurePermissionResourceProviders.AzureRoleAssignmentPermission:
                    returnValue = new AzureRoleAssignmentPermission();
                    break;
                case AzurePermissionResourceProviders.AzureKeyVaultPermission:
                    returnValue = new AzureKeyVaultPermission();
                    break;
                default:
                    throw new NotSupportedException($"{resourceProvider} is not a supported resource provider for azure permission");
            }

            if (returnValue != null)
            {
                using (JsonReader objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, returnValue);
                }
            }

            return returnValue;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
