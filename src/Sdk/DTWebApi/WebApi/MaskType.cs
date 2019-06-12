using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum MaskType
    {
        [EnumMember]
        Variable = 1,

        [EnumMember]
        Regex = 2
    }
}
