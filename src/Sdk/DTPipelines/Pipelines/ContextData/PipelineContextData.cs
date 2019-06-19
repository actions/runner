using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    /// <summary>
    /// Base class for all template tokens
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(PipelineContextDataJsonConverter))]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PipelineContextData
    {
        protected PipelineContextData(Int32 type)
        {
            Type = type;
        }

        [DataMember(Name = "t", EmitDefaultValue = false)]
        internal Int32 Type { get; }

        public abstract PipelineContextData Clone();

        public abstract JToken ToJToken();
    }
}
