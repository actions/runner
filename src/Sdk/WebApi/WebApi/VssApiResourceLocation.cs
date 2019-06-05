using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using GitHub.Services.Location;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Information about the location of a REST API resource
    /// </summary>
    [DataContract]
    public class ApiResourceLocation : IEquatable<ApiResourceLocation>, ISecuredObject
    {
        /// <summary>
        /// Unique Identifier for this location
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Area name for this resource
        /// </summary>
        [DataMember]
        public String Area { get; set; }

        /// <summary>
        /// Resource name
        /// </summary>
        [DataMember]
        public String ResourceName { get; set; }

        /// <summary>
        /// This location's route template (templated relative path)
        /// </summary>
        [DataMember]
        public String RouteTemplate { get; set; }

        /// <summary>
        /// The name of the route (not serialized to the client)
        /// </summary>
        public String RouteName { get; set; }

        /// <summary>
        /// The current resource version supported by this resource location
        /// </summary>
        [DataMember]
        public Int32 ResourceVersion { get; set; }

        /// <summary>
        /// Minimum api version that this resource supports
        /// </summary>
        public Version MinVersion { get; set; }

        /// <summary>
        /// Minimum api version that this resource supports
        /// </summary>
        [DataMember(Name = "MinVersion")]
        public String MinVersionString
        {
            get
            {
                return MinVersion.ToString(2);
            }
            private set
            {
                if (String.IsNullOrEmpty(value))
                {
                    MinVersion = new Version(1, 0);
                }
                else
                {
                    MinVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// Maximum api version that this resource supports (current server version for this resource)
        /// </summary>
        public Version MaxVersion { get; set; }

        /// <summary>
        /// Maximum api version that this resource supports (current server version for this resource)
        /// </summary>
        [DataMember(Name = "MaxVersion")]
        public String MaxVersionString
        {
            get
            {
                return MaxVersion.ToString(2);
            }
            private set
            {
                if (String.IsNullOrEmpty(value))
                {
                    MaxVersion = new Version(1, 0);
                }
                else
                {
                    MaxVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// The latest version of this resource location that is in "Release" (non-preview) mode
        /// </summary>
        public Version ReleasedVersion { get; set; }

        /// <summary>
        /// The latest version of this resource location that is in "Release" (non-preview) mode
        /// </summary>
        [DataMember(Name = "ReleasedVersion")]
        public String ReleasedVersionString
        {
            get
            {
                return ReleasedVersion.ToString(2);
            }
            private set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ReleasedVersion = new Version(1, 0);
                }
                else
                {
                    ReleasedVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ServiceDefinition ToServiceDefinition(InheritLevel level = InheritLevel.None)
        {
            return new ServiceDefinition()
            {
                Identifier = this.Id,
                ServiceType = this.Area,
                DisplayName = this.ResourceName,
                Description = "Resource Location",
                RelativePath = this.RouteTemplate,
                ResourceVersion = this.ResourceVersion,
                MinVersion = this.MinVersion,
                MaxVersion = this.MaxVersion,
                ReleasedVersion = this.ReleasedVersion,
                ToolId = "Framework", // needed for back compat for old soap clients
                InheritLevel = level
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static ApiResourceLocation FromServiceDefinition(ServiceDefinition definition)
        {
            return new ApiResourceLocation()
            {
                Id = definition.Identifier,
                Area = definition.ServiceType,
                ResourceName = definition.DisplayName,
                RouteTemplate = definition.RelativePath,
                ResourceVersion = definition.ResourceVersion,
                MinVersion = definition.MinVersion,
                MaxVersion = definition.MaxVersion,
                ReleasedVersion = definition.ReleasedVersion,
            };
        }

        public bool Equals(ApiResourceLocation other)
        {
            return (Guid.Equals(Id, other.Id) &&
                    string.Equals(Area, other.Area) &&
                    string.Equals(ResourceName, other.ResourceName) &&
                    string.Equals(RouteTemplate, other.RouteTemplate) &&
                    string.Equals(RouteName, other.RouteName) &&
                    Version.Equals(ResourceVersion, other.ResourceVersion) &&
                    Version.Equals(MinVersion, other.MinVersion) &&
                    Version.Equals(MaxVersion, other.MaxVersion) &&
                    Version.Equals(ReleasedVersion, other.ReleasedVersion));
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => LocationSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => LocationSecurityConstants.Read;

        string ISecuredObject.GetToken() => LocationSecurityConstants.NamespaceRootToken;
        #endregion
    }
}
