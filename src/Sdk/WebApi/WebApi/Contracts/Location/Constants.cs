using GitHub.Services.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Services.Location
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public enum RelativeToSetting
    {
        [EnumMember]
        Context = 0,

        [EnumMember]
        WebApplication = 2,

        [EnumMember]
        FullyQualified = 3
    }

    [DataContract]
    public enum ServiceStatus : byte
    {
        [EnumMember]
        Assigned = 0,

        [EnumMember]
        Active = 1,

        [EnumMember]
        Moving = 2,
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public enum InheritLevel : byte
    {
        None = 0,

        // The definition is visible on the deployment
        Deployment = 1,

        // The definition is visible on every account (unless overridden)
        Account = 2,

        // The definition is visible on every collection (unless overridden)
        Collection = 4,

        All = Deployment | Account | Collection
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ServiceInterfaces
    {
        public const String LocationService2 = "LocationService2";
        public const String VsService = "VsService";
        public const String VirtualLocation = "VirtualLocation";
    }

    public static class LocationServiceConstants
    {
        /// <summary>
        /// If a Location Service has an entry for an application location service, that
        /// location service definition will have an identifier of this value.
        /// </summary>
        public static readonly Guid ApplicationIdentifier = new Guid("8d299418-9467-402b-a171-9165e2f703e2");

        /// <summary>
        /// Pointer to the root location service instance
        /// </summary>
        public static readonly Guid RootIdentifier = new Guid("951917AC-A960-4999-8464-E3F0AA25B381");


        /// <summary>
        /// All Location Services have a reference to their own service definition.  That
        /// service definition has an identifier of this value.
        /// </summary>
        public static readonly Guid SelfReferenceIdentifier = new Guid("464CCB8D-ABAF-4793-B927-CFDC107791EE");
    }

    [GenerateAllConstants]
    public static class AccessMappingConstants
    {
        public static readonly string PublicAccessMappingMoniker = "PublicAccessMapping";
    }
}
