using Microsoft.VisualStudio.Services.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // This will not be like MS.TF.Framework.Common!
    // If your service does not ship in SPS or the Framework SDK you cannot put your stuff here!
    // It goes in your own assembly!
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    [GenerateAllConstants]
    public static class ServiceInstanceTypes
    {
        // !!!!!!!!!!!!!!!!!!
        // This class is sealed to new guids -- please define your instance type constant in your own assembly
        // !!!!!!!!!!!!!!!!!!

        public const String MPSString = "00000000-0000-8888-8000-000000000000";
        public static readonly Guid MPS = new Guid(MPSString);

        public const String SPSString = "951917AC-A960-4999-8464-E3F0AA25B381";
        public static readonly Guid SPS = new Guid(SPSString);

        public const String TFSString = "00025394-6065-48CA-87D9-7F5672854EF7";
        public static readonly Guid TFS = new Guid(TFSString);

        public const String TFSOnPremisesString = "87966EAA-CB2A-443F-BE3C-47BD3B5BF3CB";
        public static readonly Guid TFSOnPremises = new Guid(TFSOnPremisesString);

        [Obsolete]
        public const String SpsExtensionString = "00000024-0000-8888-8000-000000000000";
        [Obsolete]
        public static readonly Guid SpsExtension = new Guid(SpsExtensionString);

        public const String SDKSampleString = "FFFFFFFF-0000-8888-8000-000000000000";
        public static readonly Guid SDKSample = new Guid(SDKSampleString);        

        // !!!!!!!!!!!!!!!!!!
        // This class is sealed to new guids -- please define your instance type constant in your own assembly
        // !!!!!!!!!!!!!!!!!!
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
