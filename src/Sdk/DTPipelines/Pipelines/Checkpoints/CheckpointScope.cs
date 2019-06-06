using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.Pipelines.Checkpoints
{
    /// <summary>
    /// Provides context regarding the state of the orchestration.
    /// Consumers may choose to use this information to cache decisions.
    /// EG, if you wanted to return the same decision for this and all
    ///   future requests issuing from the same project / pipeline / stage / run
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class CheckpointScope
    {
        /// <summary>
        /// May be used in uniquely identify this scope for future reference.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Id { get; set; }

        /// <summary>
        /// The friendly name of the scope
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class GraphNodeScope : CheckpointScope
    {
        /// <summary>
        /// Facilitates approving only a single attempt of a graph node in a specific run of a pipeline.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Int32 Attempt { get; set; } = 1;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class PipelineScope : CheckpointScope
    {
        /// <summary>
        /// Pipeline URLs
        /// </summary>
        [DataMember(IsRequired = true)]
        public TaskOrchestrationOwner Owner { get; set; }
    }
}
