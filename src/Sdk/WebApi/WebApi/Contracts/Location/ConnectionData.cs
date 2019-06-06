using GitHub.Services.Common;
using GitHub.Services.Common.Internal;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using IC = GitHub.Services.Identity;

namespace GitHub.Services.Location
{
    /// <summary>
    /// Data transfer class that holds information needed to set up a 
    /// connection with a VSS server.
    /// </summary>
    [DataContract]
    public class ConnectionData : ISecuredObject
    {
        /// <summary>
        /// The Id of the authenticated user who made this request. More information about the user can be
        /// obtained by passing this Id to the Identity service
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IC.Identity AuthenticatedUser
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The Id of the authorized user who made this request. More information about the user can be
        /// obtained by passing this Id to the Identity service
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IC.Identity AuthorizedUser
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The instance id for this host.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid InstanceId
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The id for the server.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid DeploymentId
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The type for the server Hosted/OnPremises.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DeploymentFlags DeploymentType
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The last user access for this instance.  Null if not requested specifically.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? LastUserAccess
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// Data that the location service holds.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public LocationServiceData LocationServiceData
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The virtual directory of the host we are talking to.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String WebApplicationRelativeDirectory
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static ConnectionData FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            ConnectionData obj = new ConnectionData();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "CatalogResourceId":
                            obj.m_catalogResourceId = XmlConvert.ToGuid(reader.Value);
                            break;
                        case "InstanceId":
                            obj.InstanceId = XmlConvert.ToGuid(reader.Value);
                            break;
                        case "ServerCapabilities":
                            obj.m_serverCapabilities = XmlConvert.ToInt32(reader.Value);
                            break;
                        case "WebApplicationRelativeDirectory":
                            obj.WebApplicationRelativeDirectory = reader.Value;
                            break;
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "AuthenticatedUser":
                            obj.AuthenticatedUser = IC.Identity.FromXml(serviceProvider, reader);
                            break;
                        case "AuthorizedUser":
                            obj.AuthorizedUser = IC.Identity.FromXml(serviceProvider, reader);
                            break;
                        case "LocationServiceData":
                            obj.LocationServiceData = LocationServiceData.FromXml(serviceProvider, reader);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => LocationSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => LocationSecurityConstants.Read;

        string ISecuredObject.GetToken() => LocationSecurityConstants.NamespaceRootToken;
        #endregion

        private Guid m_catalogResourceId;
        private Int32 m_serverCapabilities;
    }

    /// <summary>
    /// Data transfer class used to transfer data about the location
    /// service data over the web service.
    /// </summary>
    [DataContract]
    public class LocationServiceData : ISecuredObject
    {
        /// <summary>
        /// The identifier of the deployment which is hosting this location data
        /// (e.g. SPS, TFS, ELS, Napa, etc.)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid ServiceOwner
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// Data about the access mappings contained by this location service.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ICollection<AccessMapping> AccessMappings
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// Data that the location service holds.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Boolean ClientCacheFresh
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The time to live on the location service cache.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(3600)]
        public Int32 ClientCacheTimeToLive
        {
            get
            {
                return m_clientCacheTimeToLive;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            set
            {
                m_clientCacheTimeToLive = value;
            }
        }

        /// <summary>
        /// The default access mapping moniker for the server.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String DefaultAccessMappingMoniker
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The obsolete id for the last change that
        /// took place on the server (use LastChangeId64).
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 LastChangeId
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The non-truncated 64-bit id for the last change that
        /// took place on the server.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int64 LastChangeId64
        {
            get
            {
                // Use obsolete truncated 32-bit value when receiving message from "old" server that doesn't provide 64-bit value
                return m_lastChangeId64 != 0 ? m_lastChangeId64 : LastChangeId;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            set
            {
                m_lastChangeId64 = value;
            }
        }

        /// <summary>
        /// Data about the service definitions contained by this location service.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ICollection<ServiceDefinition> ServiceDefinitions
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => LocationSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => LocationSecurityConstants.Read;

        string ISecuredObject.GetToken() => LocationSecurityConstants.NamespaceRootToken;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static LocationServiceData FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            LocationServiceData obj = new LocationServiceData();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "AccessPointsDoNotIncludeWebAppRelativeDirectory":
                            obj.m_accessPointsDoNotIncludeWebAppRelativeDirectory = XmlConvert.ToBoolean(reader.Value);
                            break;
                        case "ClientCacheFresh":
                            obj.ClientCacheFresh = XmlConvert.ToBoolean(reader.Value);
                            break;
                        case "DefaultAccessMappingMoniker":
                            obj.DefaultAccessMappingMoniker = reader.Value;
                            break;
                        case "LastChangeId":
                            obj.LastChangeId = XmlConvert.ToInt32(reader.Value);
                            break;
                        case "ClientCacheTimeToLive":
                            obj.ClientCacheTimeToLive = XmlConvert.ToInt32(reader.Value);
                            break;
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }                    
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "AccessMappings":
                            obj.AccessMappings = XmlUtility.ArrayOfObjectFromXml<AccessMapping>(serviceProvider, reader, "AccessMapping", false, AccessMapping.FromXml);
                            break;
                        case "ServiceDefinitions":
                            obj.ServiceDefinitions = XmlUtility.ArrayOfObjectFromXml<ServiceDefinition>(serviceProvider, reader, "ServiceDefinition", false, ServiceDefinition.FromXml);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        private Int32 m_clientCacheTimeToLive = 3600;
        private Boolean m_accessPointsDoNotIncludeWebAppRelativeDirectory;
        private Int64 m_lastChangeId64;
    }
}
