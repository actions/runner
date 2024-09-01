using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    // do NOT add new enum since it will break backward compatibility with GHES
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
