using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Checkpoints
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class CheckpointContext
    {
        /// <summary>
        /// Unique id of the checkpoint, also used as the timeline record id
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid Id { get; set; }

        /// <summary>
        /// Auth token for querying DistributedTask
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Token { get; set; }

        /// <summary>
        /// Checkpoint Instance Id
        /// Use this for sending decision events and tracing telemetry.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String OrchestrationId { get; set; }

        /// <summary>
        /// PlanId
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid PlanId { get; set; }

        /// <summary>
        /// Which TaskHub to use when sending decision events;
        /// Use this for sending decision events.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String HubName { get; set; }

        /// <summary>
        /// The project requesting decision.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public CheckpointScope Project { get; set; }

        /// <summary>
        /// The pipeline (definition) requesting decision.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public PipelineScope Pipeline { get; set; }

        /// <summary>
        /// The graph node requesting decision.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GraphNodeScope GraphNode { get; set; }
    }
}
