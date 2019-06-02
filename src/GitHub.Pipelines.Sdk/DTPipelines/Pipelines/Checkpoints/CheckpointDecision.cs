using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Checkpoints
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class CheckpointDecision
    {
        /// <summary>
        /// Checkpoint id, provided on context
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid Id { get; set; }

        /// <summary>
        /// Decision
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Result { get; set; }

        /// <summary>
        /// Additional information (optional)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Message { get; set; }

        // Decision possibilities
        public const String Approved = "Approved";
        public const String Denied = "Denied";
        public const String Canceled = "Canceled";
    }
}
