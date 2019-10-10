using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    internal sealed class ActionStepDefinitionReferenceConverter : VssSecureJsonConverter
    {
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Step).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(
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
            if (value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken actionTypeValue))
            {
                ActionSourceType actionType;
                if (actionTypeValue.Type == JTokenType.Integer)
                {
                    actionType = (ActionSourceType)(Int32)actionTypeValue;
                }
                else if (actionTypeValue.Type != JTokenType.String || !Enum.TryParse((String)actionTypeValue, true, out actionType))
                {
                    return null;
                }

                ActionStepDefinitionReference reference = null;
                switch (actionType)
                {
                    case ActionSourceType.Repository:
                        reference = new RepositoryPathReference();
                        break;

                    case ActionSourceType.ContainerRegistry:
                        reference = new ContainerRegistryReference();
                        break;

                    case ActionSourceType.Script:
                        reference = new ScriptReference();
                        break;
                }

                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, reference);
                }

                return reference;
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
