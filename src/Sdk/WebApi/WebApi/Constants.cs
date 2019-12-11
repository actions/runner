using GitHub.Services.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [GenerateAllConstants]
    public static class ServiceInstanceTypes
    {
        public const String TFSOnPremisesString = "87966EAA-CB2A-443F-BE3C-47BD3B5BF3CB";
        public static readonly Guid TFSOnPremises = new Guid(TFSOnPremisesString);
    }   

    /// <summary>
    ///     Enumeration of the options that can be passed in on Connect.
    /// </summary>
    [DataContract]
    [Flags]
    public enum ConnectOptions
    {
        /// <summary>
        /// Retrieve no optional data.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Includes information about AccessMappings and ServiceDefinitions.
        /// </summary>
        [EnumMember]
        IncludeServices = 1,

        /// <summary>
        /// Includes the last user access for this host.
        /// </summary>
        [EnumMember]
        IncludeLastUserAccess = 2,

        /// <summary>
        /// This is only valid on the deployment host and when true. Will only return
        /// inherited definitions.
        /// </summary>
        [EnumMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IncludeInheritedDefinitionsOnly = 4,

        /// <summary>
        /// When true will only return non inherited definitions.
        /// Only valid at non-deployment host.
        /// </summary>
        [EnumMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IncludeNonInheritedDefinitionsOnly = 8,
    }

    [DataContract]
    [Flags]
    public enum DeploymentFlags
    {
        [EnumMember]
        None = 0x0,

        [EnumMember]
        Hosted = 0x1,

        [EnumMember]
        OnPremises = 0x2
    }
}
