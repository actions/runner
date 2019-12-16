using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    internal sealed class StepConverter : VssSecureJsonConverter
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
            if (value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken stepTypeValue))
            {
                StepType stepType;
                if (stepTypeValue.Type == JTokenType.Integer)
                {
                    stepType = (StepType)(Int32)stepTypeValue;
                }
                else if (stepTypeValue.Type != JTokenType.String || !Enum.TryParse((String)stepTypeValue, true, out stepType))
                {
                    return null;
                }

                Step stepObject = null;
                switch (stepType)
                {
                    case StepType.Action:
                        stepObject = new ActionStep();
                        break;
                }

                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, stepObject);
                }

                return stepObject;
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
