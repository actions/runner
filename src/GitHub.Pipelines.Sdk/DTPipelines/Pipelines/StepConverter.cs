using System;
using System.Reflection;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
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
            if (!value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken stepTypeValue))
            {
                Step compatStepObject;
                if (value.TryGetValue("Parameters", StringComparison.OrdinalIgnoreCase, out _))
                {
                    compatStepObject = new TaskTemplateStep();
                }
                else
                {
                    compatStepObject = new TaskStep();
                }

                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, compatStepObject);
                }

                return compatStepObject;
            }
            else
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

                    case StepType.Group:
                        stepObject = new GroupStep();
                        break;

#pragma warning disable CS0618 // Type or member is obsolete
                    case StepType.Script:
                        stepObject = new ScriptStep();
                        break;
#pragma warning restore CS0618 // Type or member is obsolete

                    case StepType.Task:
                        stepObject = new TaskStep();
                        break;

                    case StepType.TaskTemplate:
                        stepObject = new TaskTemplateStep();
                        break;
                }

                using (var objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, stepObject);
                }

                return stepObject;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
