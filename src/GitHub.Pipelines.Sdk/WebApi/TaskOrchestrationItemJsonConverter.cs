using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal sealed class TaskOrchestrationItemJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(TaskOrchestrationItem).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
            {
                return existingValue;
            }

            JsonProperty property = contract.Properties.GetClosestMatchProperty("ItemType");
            if (property == null)
            {
                return existingValue;
            }

            JToken itemTypeValue;
            TaskOrchestrationItemType itemType;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue(property.PropertyName, out itemTypeValue))
            {
                return existingValue;
            }
            else
            {
                if (itemTypeValue.Type == JTokenType.Integer)
                {
                    itemType = (TaskOrchestrationItemType)(Int32)itemTypeValue;
                }
                else if (itemTypeValue.Type != JTokenType.String ||
                         !Enum.TryParse<TaskOrchestrationItemType>((String)itemTypeValue, true, out itemType))
                {
                    return existingValue;
                }
            }

            Object newValue = null;
            switch (itemType)
            {
                case TaskOrchestrationItemType.Container:
                    newValue = new TaskOrchestrationContainer();
                    break;

                case TaskOrchestrationItemType.Job:
                    newValue = new TaskOrchestrationJob();
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
            // The virtual method returns false for CanWrite so this should never be invoked
            throw new NotSupportedException();
        }
    }
}
