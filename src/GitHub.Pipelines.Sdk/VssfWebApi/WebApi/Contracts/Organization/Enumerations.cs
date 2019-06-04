using System.Runtime.Serialization;

namespace GitHub.Services.Organization
{
    [DataContract]
    public enum CollectionSearchKind
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        ById = 1,

        [EnumMember]
        ByName = 2,

        [EnumMember]
        ByTenantId = 3,
    }

    [DataContract]
    public enum OrganizationSearchKind
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        ById = 1,

        [EnumMember]
        ByName = 2,

        [EnumMember]
        ByTenantId = 3,
    }

    [DataContract]
    public enum OrganizationType
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        Personal = 1,

        [EnumMember]
        Work = 2,
    }

    [DataContract]
    public enum OrganizationStatus
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        Initial = 10,

        [EnumMember]
        Enabled = 20,

        [EnumMember]
        MarkedForDelete = 30,
    }

    [DataContract]
    public enum CollectionStatus
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        Initial = 10,

        [EnumMember]
        Enabled = 20,

        [EnumMember]
        LogicallyDeleted = 30,

        [EnumMember]
        MarkedForPhysicalDelete = 40, 
    }

    [DataContract]
    public enum AssignmentStatus
    {
        Unassignable = 0,
        Assignable   = 10,
        Assigning    = 20,
        Assigned     = 30,
    }

    [DataContract]
    public enum HostCreationType
    {
        None        = 0,
        PreCreated  = 1,
        OnDemand    = 2,
    }
}
