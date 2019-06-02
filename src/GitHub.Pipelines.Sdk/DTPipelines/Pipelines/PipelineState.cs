using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum PipelineState
    {
        [EnumMember]
        NotStarted,

        [EnumMember]
        InProgress,

        [EnumMember]
        Canceling,

        [EnumMember]
        Completed,
    }
}
