using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskDefinitionStatus
    {
        [EnumMember]
        Preinstalled = 1,

        [EnumMember]
        ReceivedInstallOrUpdate = 2,

        [EnumMember]
        Installed = 3,

        [EnumMember]
        ReceivedUninstall = 4,

        [EnumMember]
        Uninstalled = 5,

        [EnumMember]
        RequestedUpdate = 6,

        [EnumMember]
        Updated = 7,

        [EnumMember]
        AlreadyUpToDate = 8,

        [EnumMember]
        InlineUpdateReceived = 9,
    }
}
