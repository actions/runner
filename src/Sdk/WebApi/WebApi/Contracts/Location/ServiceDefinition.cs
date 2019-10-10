using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Location
{
    /// <summary>
    /// 
    /// </summary>
    [DebuggerDisplay("{ServiceType}:{Identifier}")]
    [DataContract]
    public class ServiceDefinition : ISecuredObject
    {
        public ServiceDefinition()
        {
            LocationMappings = new List<LocationMapping>();
            Status = ServiceStatus.Active;
            Properties = new PropertiesCollection();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ServiceDefinition(
            String serviceType,
            Guid identifier,
            String displayName,
            String relativePath,
            RelativeToSetting relativeToSetting,
            String description,
            String toolId,
            List<LocationMapping> locationMappings = null,
            Guid serviceOwner = new Guid())
        {
            ServiceType = serviceType;
            Identifier = identifier;
            DisplayName = displayName;
            RelativePath = relativePath;
            RelativeToSetting = relativeToSetting;
            Description = description;
            ToolId = toolId;

            if (locationMappings == null)
            {
                locationMappings = new List<LocationMapping>();
            }

            LocationMappings = locationMappings;
            ServiceOwner = serviceOwner;
            Properties = new PropertiesCollection();
            Status = ServiceStatus.Active;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("serviceType")] // XML Attribute is required for servicing xml de-serialization
        public String ServiceType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("identifier")] // XML Attribute is required for servicing xml de-serialization
        public Guid Identifier
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("displayName")] // XML Attribute is required for servicing xml de-serialization
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public RelativeToSetting RelativeToSetting
        {
            get;
            set;
        }

        [XmlAttribute("relativeToSetting")] // XML Attribute is required for servicing xml de-serialization
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 RelativeToSettingValue
        {
            get
            {
                return (Int32)RelativeToSetting;
            }
            set
            {
                RelativeToSetting = (RelativeToSetting)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("relativePath")] // XML Attribute is required for servicing xml de-serialization
        public String RelativePath
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("description")] // XML Attribute is required for servicing xml de-serialization
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The service which owns this definition e.g. TFS, ELS, etc.
        /// </summary>
        [DataMember]
        public Guid ServiceOwner 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<LocationMapping> LocationMappings
        {
            get;
            set;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("toolId")] // XML Attribute is required for servicing xml de-serialization
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ToolId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ParentServiceType
        {
            get;
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid ParentIdentifier
        {
            get;
            set;
        }

        [DefaultValue(ServiceStatus.Active)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ServiceStatus Status
        {
            get;
            set;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("inheritLevel")]
        public InheritLevel InheritLevel
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlIgnore]
        public PropertiesCollection Properties
        {
            get;
            set;
        }

        //*****************************************************************************************************************
        /// <summary>
        /// Generic Property accessor. Returns default value of T if not found
        /// </summary>
        //*****************************************************************************************************************
        public T GetProperty<T>(String name, T defaultValue)
        {
            T value;
            if (Properties != null && Properties.TryGetValue<T>(name, out value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        //*****************************************************************************************************************
        /// <summary>
        /// Property accessor. value will be null if not found.
        /// </summary>
        //*****************************************************************************************************************
        public Boolean TryGetProperty(String name, out Object value)
        {
            value = null;
            return Properties == null ? false : Properties.TryGetValue(name, out value);
        }

        //*****************************************************************************************************************
        /// <summary>
        /// Internal function to initialize persisted property.
        /// </summary>
        //*****************************************************************************************************************
        public void SetProperty(String name, Object value)
        {
            m_hasModifiedProperties = true;

            //don't remove properties with null
            //vals, just set them to null...
            Properties[name] = value;
        }

        //*****************************************************************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //*****************************************************************************************************************
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean HasModifiedProperties
        {
            get
            {
                return m_hasModifiedProperties;
            }
        }

        //*****************************************************************************************************************
        /// <summary>
        /// 
        /// </summary>
        //*****************************************************************************************************************
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetModifiedProperties()
        {
            m_hasModifiedProperties = false;
        }

        /// <summary>
        /// The current resource version supported by this resource location. Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlAttribute("resourceVersion")]
        [DefaultValue(0)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 ResourceVersion { get; set; }

        /// <summary>
        /// Minimum api version that this resource supports. Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlIgnore]
        public Version MinVersion { get; set; }

        /// <summary>
        /// Minimum api version that this resource supports. Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlAttribute("minVersion")]
        [DefaultValue(null)]
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "MinVersion")]
        public String MinVersionString
        {
            get
            {
                if (MinVersion == null)
                {
                    return null;
                }
                else
                {
                    return MinVersion.ToString(2);
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    MinVersion = null;
                }
                else
                {
                    MinVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// Maximum api version that this resource supports (current server version for this resource). Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlIgnore]
        public Version MaxVersion { get; set; }

        /// <summary>
        /// Maximum api version that this resource supports (current server version for this resource). Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlAttribute("maxVersion")]
        [DefaultValue(null)]
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "MaxVersion")]
        public String MaxVersionString
        {
            get
            {
                if (MaxVersion == null)
                {
                    return null;
                }
                else
                {            
                    return MaxVersion.ToString(2);
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    MaxVersion = null;
                }
                else
                {
                    MaxVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// The latest version of this resource location that is in "Release" (non-preview) mode. Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlIgnore]
        public Version ReleasedVersion { get; set; }

        /// <summary>
        /// The latest version of this resource location that is in "Release" (non-preview) mode. Copied from <c>ApiResourceLocation</c>.
        /// </summary>
        [XmlAttribute("releasedVersion")]
        [DefaultValue(null)]
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "ReleasedVersion")]
        public String ReleasedVersionString
        {
            get
            {
                if (ReleasedVersion == null)
                {
                    return null;
                }
                else
                {            
                    return ReleasedVersion.ToString(2);
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ReleasedVersion = null;
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
        public ServiceDefinition Clone()
        {
            return Clone(true);
        }

        public ServiceDefinition Clone(Boolean includeLocationMappings)
        {
            List<LocationMapping> locationMappings = null;

            if (LocationMappings != null && includeLocationMappings)
            {
                locationMappings = new List<LocationMapping>(LocationMappings.Count);

                foreach (LocationMapping mapping in LocationMappings)
                {
                    locationMappings.Add(new LocationMapping()
                    {
                        AccessMappingMoniker = mapping.AccessMappingMoniker,
                        Location = mapping.Location
                    });
                }
            }
            else
            {
                locationMappings = new List<LocationMapping>();
            }

            PropertiesCollection properties = null;

            if (Properties != null)
            {
                // since we are cloning, don't validate the values
                properties = new PropertiesCollection(Properties, validateExisting: false);
            }
            else
            {
                properties = new PropertiesCollection();
            }

            ServiceDefinition serviceDefinition = new ServiceDefinition()
            {
                ServiceType = ServiceType,
                Identifier = Identifier,
                DisplayName = DisplayName,
                RelativePath = RelativePath,
                RelativeToSetting = RelativeToSetting,
                Description = Description,
                LocationMappings = locationMappings,
                ServiceOwner = ServiceOwner,
                ToolId = ToolId,
                ParentServiceType = ParentServiceType,
                ParentIdentifier = ParentIdentifier,
                Status = Status,
                Properties = properties,
                ResourceVersion = ResourceVersion,
                MinVersion = MinVersion,
                MaxVersion = MaxVersion,
                ReleasedVersion = ReleasedVersion
            };

            serviceDefinition.ResetModifiedProperties();
            return serviceDefinition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static ServiceDefinition FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            ServiceDefinition obj = new ServiceDefinition();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "description":
                            obj.Description = reader.Value;
                            break;
                        case "displayName":
                            obj.DisplayName = reader.Value;
                            break;
                        case "identifier":
                            obj.Identifier = XmlConvert.ToGuid(reader.Value);
                            break;
                        case "isSingleton":
                            obj.m_isSingleton = XmlConvert.ToBoolean(reader.Value);
                            break;
                        case "relativePath":
                            obj.RelativePath = reader.Value;
                            break;
                        case "relativeToSetting":
                            obj.RelativeToSetting = (RelativeToSetting)XmlConvert.ToInt32(reader.Value);
                            break;
                        case "serviceType":
                            obj.ServiceType = reader.Value;
                            break;
                        case "toolId":
                            obj.ToolId = reader.Value;
                            break;
                        case "resourceVersion":
                            obj.ResourceVersion = XmlConvert.ToInt32(reader.Value);
                            break;
                        case "minVersion":
                            obj.MinVersionString = reader.Value;
                            break;
                        case "maxVersion":
                            obj.MaxVersionString = reader.Value;
                            break;
                        case "releasedVersion":
                            obj.ReleasedVersionString = reader.Value;
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
                        case "LocationMappings":
                            obj.LocationMappings = new List<LocationMapping>(XmlUtility.ArrayOfObjectFromXml<LocationMapping>(serviceProvider, reader, "LocationMapping", false, LocationMapping.FromXml));
                            break;
                        case "Properties":
                            // Ignore properties
                            reader.ReadOuterXml();
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

        /// <summary>
        ///     Returns the LocationMapping for the AccessMapping provided or null
        ///     if this ServiceDefinition does not have a LocationMapping for the provided
        ///     AccessMapping. This function will always return null if it is called
        ///     on a non-relative ServiceDefinition.
        /// </summary>
        /// <param name="accessMapping">
        ///     The AccessMapping to find the LocationMapping for.
        /// </param>
        /// <returns>
        ///     The LocationMapping for the AccessMapping provided or null if this 
        ///     ServiceDefinition does not have a LocationMapping for the provided
        ///     AccessMapping. This function will always return null if it is called
        ///     on a non-relative ServiceDefinition.
        /// </returns>
        public LocationMapping GetLocationMapping(AccessMapping accessMapping)
        {
            ArgumentUtility.CheckForNull(accessMapping, "accessMapping");

            return GetLocationMapping(accessMapping.Moniker);
        }

        public LocationMapping GetLocationMapping(String accessMappingMoniker)
        {
            ArgumentUtility.CheckForNull(accessMappingMoniker, "accessMappingMoniker");

            // If this is FullyQualified then look through our location mappings
            if (RelativeToSetting == RelativeToSetting.FullyQualified)
            {
                foreach (LocationMapping locationMapping in LocationMappings)
                {
                    if (VssStringComparer.AccessMappingMoniker.Equals(locationMapping.AccessMappingMoniker, accessMappingMoniker))
                    {
                        return locationMapping;
                    }
                }
            }

            // We weren't able to find the location for the access mapping. Return null.
            return null;
        }

        /// <summary>
        /// Adds a location mapping for the provided access mapping and location
        /// to the service definition.  Note that if a mapping already exists for
        /// the provided access mapping, it will be overwritten.
        /// </summary>
        /// <param name="accessMapping">The access mapping this location mapping is for.
        /// This access mapping must already be registered in the LocationService.  To create
        /// a new access mapping, see LocationService.ConfigureAccessMapping</param>
        /// <param name="location">This value must be null if the RelativeToSetting
        /// for this ServiceDefinition is something other than FullyQualified.  If
        /// this ServiceDefinition has a RelativeToSetting of FullyQualified, this
        /// value must not be null and should be the location where this service resides
        /// for this access mapping.</param>
        public void AddLocationMapping(AccessMapping accessMapping, String location)
        {
            if (RelativeToSetting != RelativeToSetting.FullyQualified)
            {
                throw new InvalidOperationException(WebApiResources.RelativeLocationMappingErrorMessage());
            }

            // Make sure the location has a value 
            if (location == null)
            {
                throw new ArgumentException(WebApiResources.FullyQualifiedLocationParameter());
            }

            // See if an entry for this access mapping already exists, if it does, overwrite it.
            foreach (LocationMapping mapping in LocationMappings)
            {
                if (VssStringComparer.AccessMappingMoniker.Equals(mapping.AccessMappingMoniker, accessMapping.Moniker))
                {
                    mapping.Location = location;
                    return;
                }
            }

            // This is a new entry for this access mapping, just add it.
            LocationMappings.Add(new LocationMapping() { AccessMappingMoniker = accessMapping.Moniker, Location = location });
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => LocationSecurityConstants.NamespaceId;
    
        int ISecuredObject.RequiredPermissions => LocationSecurityConstants.Read;

        string ISecuredObject.GetToken()
        {
            return LocationSecurityConstants.ServiceDefinitionsToken;
        }
        #endregion

        private Boolean m_isSingleton;
        private Boolean m_hasModifiedProperties = true;
    }
}
