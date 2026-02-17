#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.WorkflowParser
{
    internal sealed class IStepJsonConverter : JsonConverter
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
            return typeof(IStep).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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
            Object newValue = null;

            if (value.TryGetValue("Uses", StringComparison.OrdinalIgnoreCase, out _))
            {
                newValue = new ActionStep();
            }
            else if (value.TryGetValue("Run", StringComparison.OrdinalIgnoreCase, out _))
            {
                newValue = new RunStep();
            }
            else
            {
                return existingValue;
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
