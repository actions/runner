using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskAgentStatus
    {
        [EnumMember]
        Offline = 1,

        [EnumMember]
        Online = 2,
    }
}
