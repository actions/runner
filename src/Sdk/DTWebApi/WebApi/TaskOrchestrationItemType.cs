using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
