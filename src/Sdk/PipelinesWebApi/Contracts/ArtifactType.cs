using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.Actions.Pipelines.WebApi
{
    [DataContract]
    [JsonConverter(typeof(ArtifactTypeEnumJsonConverter))]
    public enum ArtifactType
    {
        Unknown = 0,
        Actions_Storage = 1
    }
} 
