using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum LabelType
    {
        [EnumMember]
        System = 0,

        [EnumMember]
        User = 1
    }
}
