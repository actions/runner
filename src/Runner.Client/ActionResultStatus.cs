using System.Runtime.Serialization;

namespace Runner.Client
{
[DataContract]
public enum ActionResultStatus
{
    [EnumMember]
    Success = 0,

    [EnumMember]
    Failure = 2,

    [EnumMember]
    Cancelled = 3,

    [EnumMember]
    Skipped = 4,
}

}
