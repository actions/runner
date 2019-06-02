using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData
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
    }
}