using System.Runtime.Serialization;

namespace Sdk.RSWebApi.Contracts
{
    [DataContract]
    public enum AnnotationLevel
    {
        [EnumMember]
        UNKNOWN = 0,

        [EnumMember]
        NOTICE = 1,

        [EnumMember]
        WARNING = 2,

        [EnumMember]
        FAILURE = 3
    }
}
