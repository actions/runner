using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http.Formatting;

namespace GitHub.Actions.Pipelines.WebApi
{
    public class ArtifactTypeEnumJsonConverter : UnknownEnumJsonConverter
    {
        //json.net v12 exposes a "NamingStrategy" member that can do all this. We are at json.net v10 which only supports camel case.
        //This is a poor man's way to fake it 
        public override void WriteJson(JsonWriter writer, object enumValue, JsonSerializer serializer)
        {
            var value = (ArtifactType)enumValue;
            if (value == ArtifactType.Actions_Storage)
            {
                writer.WriteValue("actions_storage");
            }
            else
            { 
                base.WriteJson(writer, enumValue, serializer);
            }
        }
    }
}
