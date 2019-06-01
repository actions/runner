using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum PhaseTargetType
    {
        [EnumMember]
        Queue,

        [EnumMember]
        Server,

        [EnumMember]
        DeploymentGroup,

        [EnumMember]
        Pool,
    }
}
