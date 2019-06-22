using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.Build.WebApi
{
    internal sealed class BuildTriggerJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(BuildTrigger).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

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

            JsonProperty property = contract.Properties.GetClosestMatchProperty("TriggerType");
            if (property == null)
            {
                return existingValue;
            }

            JToken itemTypeValue;
            DefinitionTriggerType triggerType;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out itemTypeValue))
            {
                return existingValue;
            }
            else
            {
                if (itemTypeValue.Type == JTokenType.Integer)
                {
                    triggerType = (DefinitionTriggerType)(Int32)itemTypeValue;
                }
                else if (itemTypeValue.Type != JTokenType.String ||
                         !Enum.TryParse<DefinitionTriggerType>((String)itemTypeValue, true, out triggerType))
                {
                    return existingValue;
                }
            }

            Object returnValue = null;
            switch (triggerType)
            {
                case DefinitionTriggerType.ContinuousIntegration:
                    returnValue = new ContinuousIntegrationTrigger();
                    break;
                case DefinitionTriggerType.GatedCheckIn:
                    returnValue = new GatedCheckInTrigger();
                    break;
                case DefinitionTriggerType.Schedule:
                    returnValue = new ScheduleTrigger();
                    break;
                case DefinitionTriggerType.PullRequest:
                    returnValue = new PullRequestTrigger();
                    break;
                case DefinitionTriggerType.BuildCompletion:
                    returnValue = new BuildCompletionTrigger();
                    break;
            }

            if (value != null && returnValue != null)
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
