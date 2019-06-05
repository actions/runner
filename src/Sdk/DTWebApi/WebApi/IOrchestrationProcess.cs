using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    [ClientIgnore]
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum OrchestrationProcessType
    {
        [DataMember]
        Container = 1,

        [DataMember]
        Pipeline = 2,
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonConverter(typeof(OrchestrationEnvironmentJsonConverter))]
    public interface IOrchestrationEnvironment
    {
        OrchestrationProcessType ProcessType { get; }

        IDictionary<String, VariableValue> Variables { get; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonConverter(typeof(OrchestrationProcessJsonConverter))]
    public interface IOrchestrationProcess
    {
        OrchestrationProcessType ProcessType { get; }
    }

    internal sealed class OrchestrationProcessJsonConverter : VssSecureJsonConverter
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
            return typeof(IOrchestrationProcess).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            JObject value = JObject.Load(reader);
            IOrchestrationProcess process = null;
            if (value.TryGetValue("stages", StringComparison.OrdinalIgnoreCase, out _) ||
                value.TryGetValue("phases", StringComparison.OrdinalIgnoreCase, out _))
            {
                process = new PipelineProcess();
            }
            else if (value.TryGetValue("children", StringComparison.OrdinalIgnoreCase, out _))
            {
                process = new TaskOrchestrationContainer();
            }

            if (process != null)
            {
                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, process);
                }
            }

            return process;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class OrchestrationEnvironmentJsonConverter : VssSecureJsonConverter
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
            return typeof(IOrchestrationEnvironment).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            JToken propertyValue;
            JObject value = JObject.Load(reader);
            IOrchestrationEnvironment environment = null;
            OrchestrationProcessType processType = OrchestrationProcessType.Container;
            if (value.TryGetValue("ProcessType", StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                if (propertyValue.Type == JTokenType.Integer)
                {
                    processType = (OrchestrationProcessType)(Int32)propertyValue;
                }
                else if (propertyValue.Type != JTokenType.String || !Enum.TryParse((String)propertyValue, true, out processType))
                {
                    return null;
                }
            }

            switch (processType)
            {
                case OrchestrationProcessType.Container:
                    environment = new PlanEnvironment();
                    break;

                case OrchestrationProcessType.Pipeline:
                    environment = new PipelineEnvironment();
                    break;
            }

            if (environment != null)
            {
                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, environment);
                }
            }

            return environment;
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
