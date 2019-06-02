using System;
using System.Reflection;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal sealed class JobRequestMessageJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(JobRequestMessage).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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
            JToken propertyValue;
            JObject value = JObject.Load(reader);

            if (value.TryGetValue("MessageType", StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                if (propertyValue.Type == JTokenType.String)
                {
                    var messageType = (String)propertyValue;

                    switch (messageType)
                    {
                        case JobRequestMessageTypes.AgentJobRequest:
                            newValue = new AgentJobRequestMessage();
                            break;

                        case JobRequestMessageTypes.ServerTaskRequest:
                        case JobRequestMessageTypes.ServerJobRequest:
                            newValue = new ServerTaskRequestMessage();
                            break;
                    }
                }
            }
           
            if (newValue == null)
            {
                if (value.TryGetValue("RequestId", StringComparison.OrdinalIgnoreCase, out propertyValue))
                {
                    newValue = new AgentJobRequestMessage();
                }
            }

            if (newValue == null)
            {
                return existingValue;
            }

            using (JsonReader objectReader = value.CreateReader())
            {
                serializer.Populate(objectReader, newValue);
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