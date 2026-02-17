#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.WorkflowParser
{
    internal sealed class IJobJsonConverter : JsonConverter
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
            return typeof(IJob).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            JobType? jobType = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken typeValue))
            {
                return existingValue;
            }
            else
            {
                if (typeValue.Type == JTokenType.Integer)
                {
                    jobType = (JobType)(int)typeValue;
                }
                else if (typeValue.Type == JTokenType.String)
                {
                    JobType parsedType;
                    if (Enum.TryParse((String)typeValue, ignoreCase: true, result: out parsedType))
                    {
                        jobType = parsedType;
                    }
                }
            }

            if (jobType == null)
            {
                return existingValue;
            }

            Object newValue = null;
            switch (jobType)
            {
                case JobType.Job:
                    newValue = new Job();
                    break;

                case JobType.ReusableWorkflowJob:
                    newValue = new ReusableWorkflowJob();
                    break;
            }

            if (value != null)
            {
                using JsonReader objectReader = value.CreateReader();
                serializer.Populate(objectReader, newValue);
            }

            return newValue;
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
