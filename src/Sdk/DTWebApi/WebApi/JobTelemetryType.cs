using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    public enum JobTelemetryType
    {
        [EnumMember]
        General = 0,

        [EnumMember]
        ActionCommand = 1,

        [EnumMember]
        ConnectivityCheck = 2,
    }
}
