using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.Pipelines
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
