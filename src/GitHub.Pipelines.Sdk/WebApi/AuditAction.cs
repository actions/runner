namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System.Runtime.Serialization;

    public enum AuditAction
    {
        [EnumMember]
        Add = 1,

        [EnumMember]
        Update = 2,

        [EnumMember]
        Delete = 3,

        [EnumMember]
        Undelete = 4
    }
}
