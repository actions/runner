using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskOrchestrationItemType
    {
        [EnumMember]
        Container,

        [EnumMember]
        Job,
    }
}
