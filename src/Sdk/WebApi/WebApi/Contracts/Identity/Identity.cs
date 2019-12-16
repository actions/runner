// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Identity
{
    //The only PATCH-able property on this class is CustomDisplayName, however there are more read/write properties
    //because they get set by various providers in the Framework dll, in general Framework should not have internalsvisibleto to this dll
    //CONSIDER: Should providers be in GitHub.Services.Identity instead?
    [DataContract]
    public sealed class Identity : IdentityBase, ISecuredObject
    {
        public Identity() : this(null)
        {
        }

        private Identity(PropertiesCollection properties) : base(properties)
        {
        }

        public Identity Clone(bool includeMemberships)
        {
            PropertiesCollection properties = new PropertiesCollection(Properties, validateExisting: false);

            Identity clone = new Identity(properties)
            {
                Id = Id,
                Descriptor = new IdentityDescriptor(Descriptor),
                ProviderDisplayName = ProviderDisplayName,
                CustomDisplayName = CustomDisplayName,
                IsActive = IsActive,
                UniqueUserId = UniqueUserId,
                IsContainer = IsContainer,
                ResourceVersion = ResourceVersion,
                MetaTypeId = MetaTypeId
            };

            if (includeMemberships)
            {
                clone.Members = CloneDescriptors(Members);
                clone.MemberOf = CloneDescriptors(MemberOf);
                clone.MemberIds = MemberIds?.ToList();
                clone.MemberOfIds = MemberOfIds?.ToList();
            }

            clone.MasterId = MasterId;

            return clone;
        }

        public Identity Clone()
        {
            return this.Clone(true);
        }

        internal static Identity FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            Identity obj = new Identity();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            bool empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "DisplayName":
                            obj.ProviderDisplayName = reader.Value;
                            break;
                        case "IsActive":
                            obj.IsActive = XmlConvert.ToBoolean(reader.Value);
                            break;
                        case "IsContainer":
                            obj.IsContainer = XmlConvert.ToBoolean(reader.Value);
                            break;
                        case "TeamFoundationId":
                            obj.Id = XmlConvert.ToGuid(reader.Value);
                            break;
                        case "UniqueName":
                            // We don't have this property on VSIdentity
                            //obj.UniqueName = reader.Value;
                            break;
                        case "UniqueUserId":
                            obj.UniqueUserId = XmlConvert.ToInt32(reader.Value);
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
                        case "Attributes":
                            KeyValueOfStringString[] attributes = XmlUtility.ArrayOfObjectFromXml<KeyValueOfStringString>(serviceProvider, reader, "KeyValueOfStringString", false, KeyValueOfStringString.FromXml);
                            if (attributes != null && obj.Properties != null)
                            {
                                foreach (KeyValueOfStringString attribute in attributes)
                                {
                                    obj.Properties[attribute.Key] = attribute.Value;
                                }
                            }
                            break;
                        case "Descriptor":
                            obj.Descriptor = IdentityDescriptor.FromXml(serviceProvider, reader);
                            break;
                        case "LocalProperties":
                            // Since we're only using the SOAP serializer for bootstrap, we won't support properties
                            //obj.m_localPropertiesSet = Helper.ArrayOfPropertyValueFromXml(serviceProvider, reader, false);
                            reader.ReadOuterXml();
                            break;
                        case "MemberOf":
                            obj.MemberOf = XmlUtility.ArrayOfObjectFromXml<IdentityDescriptor>(serviceProvider, reader, "IdentityDescriptor", false, IdentityDescriptor.FromXml);
                            break;
                        case "Members":
                            obj.Members = XmlUtility.ArrayOfObjectFromXml<IdentityDescriptor>(serviceProvider, reader, "IdentityDescriptor", false, IdentityDescriptor.FromXml);
                            break;
                        case "Properties":
                            // Since we're only using the SOAP serializer for bootstrap, we won't support properties
                            //obj.m_propertiesSet = Helper.ArrayOfPropertyValueFromXml(serviceProvider, reader, false);
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

        #region ISecuredObject
        public Guid NamespaceId => GraphSecurityConstants.NamespaceId;

        public int RequiredPermissions => GraphSecurityConstants.ReadByPublicIdentifier;

        public string GetToken() => GraphSecurityConstants.SubjectsToken;
        #endregion

        private static ICollection<IdentityDescriptor> CloneDescriptors(IEnumerable<IdentityDescriptor> descriptors)
        {
            return descriptors?.Select(item => new IdentityDescriptor(item)).ToList();
        }
    }

    /// <summary>
    /// Base Identity class to allow "trimmed" identity class in the GetConnectionData API
    /// Makes sure that on-the-wire representations of the derived classes are compatible with each other
    /// (e.g. Server responds with PublicIdentity object while client deserializes it as Identity object)
    /// Derived classes should not have additional [DataMember] properties
    /// </summary>
    [DebuggerDisplay("Name: {DisplayName} ID:{Id}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public abstract class IdentityBase : IVssIdentity
    {
        protected IdentityBase(PropertiesCollection properties)
        {
            if (properties == null)
            {
                Properties = new PropertiesCollection();
            }
            else
            {
                Properties = properties;
            }

            ResourceVersion = IdentityConstants.DefaultResourceVersion;

            // Initialize this as Unknown (255) so the default integer value of MetaTypeId isn't set as Member (0)
            MetaType = IdentityMetaType.Unknown;
        }

        [DataMember]
        public Guid Id
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IdentityDescriptor Descriptor
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        //*****************************************************************************************************************
        /// <summary>
        /// The display name for the identity as specified by the source identity provider.
        /// </summary>
        //*****************************************************************************************************************
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string ProviderDisplayName
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        //*****************************************************************************************************************
        /// <summary>
        /// The custom display name for the identity (if any). Setting this property to an empty string will clear the existing
        /// custom display name. Setting this property to null will not affect the existing persisted value
        /// (since null values do not get sent over the wire or to the database)
        /// </summary>
        //*****************************************************************************************************************
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string CustomDisplayName { get; set; }

        //*****************************************************************************************************************
        /// <summary>
        /// This is a computed property equal to the CustomDisplayName (if set) or the ProviderDisplayName.
        /// </summary>
        //*****************************************************************************************************************
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomDisplayName))
                {
                    return CustomDisplayName;
                }

                return ProviderDisplayName;
            }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsActive { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int UniqueUserId
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsContainer
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ICollection<IdentityDescriptor> Members
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ICollection<IdentityDescriptor> MemberOf
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICollection<Guid> MemberIds { get; set; }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICollection<Guid> MemberOfIds { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid MasterId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public PropertiesCollection Properties { get; private set; }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ValidateProperties
        {
            get
            {
                return this.Properties.ValidateNewValues;
            }
            set
            {
                this.Properties.ValidateNewValues = value;
            }
        }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsExternalUser
        {
            get
            {
                Guid domain;
                return Guid.TryParse(GetProperty(IdentityAttributeTags.Domain, string.Empty), out domain) &&
                    Descriptor.IdentityType != IdentityConstants.ServiceIdentityType &&
                    Descriptor.IdentityType != IdentityConstants.AggregateIdentityType &&
                    Descriptor.IdentityType != IdentityConstants.ImportedIdentityType;
            }
        }

        /// <summary>
        /// Get the Id of the containing scope
        /// </summary>
        [IgnoreDataMember]
        public Guid LocalScopeId
        {
            get
            {
                return GetProperty(IdentityAttributeTags.LocalScopeId, default(Guid));
            }
        }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsBindPending =>
            this.Descriptor != null &&
            IdentityConstants.BindPendingIdentityType.Equals(Descriptor.IdentityType);

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsClaims =>
            this.Descriptor != null &&
            IdentityConstants.ClaimsType.Equals(Descriptor.IdentityType);

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsImported =>
            this.Descriptor != null &&
            IdentityConstants.ImportedIdentityType.Equals(Descriptor.IdentityType);

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsServiceIdentity =>
            this.Descriptor != null &&
            IdentityConstants.ServiceIdentityType.Equals(Descriptor.IdentityType);

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int ResourceVersion { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public int MetaTypeId { get; set; }

        public IdentityMetaType MetaType
        {
            get { return (IdentityMetaType)MetaTypeId; }
            set { MetaTypeId = (int)value; }
        }

        [IgnoreDataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsCspPartnerUser =>
            this.Descriptor != null &&
            this.Descriptor.IsCspPartnerIdentityType();

        //*****************************************************************************************************************
        /// <summary>
        /// Generic Property accessor. Returns default value of T if not found
        /// </summary>
        //*****************************************************************************************************************
        public T GetProperty<T>(string name, T defaultValue)
        {
            if (Properties != null && Properties.TryGetValidatedValue(name, out T value))
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
        public bool TryGetProperty(string name, out object value)
        {
            value = null;
            return Properties != null && Properties.TryGetValue(name, out value);
        }

        //*****************************************************************************************************************
        /// <summary>
        /// Internal function to initialize persisted property.
        /// </summary>
        //*****************************************************************************************************************
        public void SetProperty(string name, object value)
        {
            m_hasModifiedProperties = true;

            //don't remove properties with null
            //vals, just set them to null...
            Properties[name] = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasModifiedProperties => m_hasModifiedProperties;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetModifiedProperties()
        {
            m_hasModifiedProperties = false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetAllModifiedProperties()
        {
            m_hasModifiedProperties = true;
        }

        public override bool Equals(object obj)
        {
            IdentityBase other = obj as IdentityBase;
            if (other != null)
            {
                return (Id == other.Id &&
                    IdentityDescriptorComparer.Instance.Equals(Descriptor, other.Descriptor) &&
                    string.Equals(ProviderDisplayName, other.ProviderDisplayName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(CustomDisplayName, other.CustomDisplayName, StringComparison.OrdinalIgnoreCase) &&
                    IsActive == other.IsActive &&
                    UniqueUserId == other.UniqueUserId &&
                    IsContainer == other.IsContainer);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (Descriptor == null)
            {
                return 0;
            }
            return Descriptor.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Identity {0} (IdentityType: {1}; Identifier: {2}; DisplayName: {3})",
                Id,
                (Descriptor == null) ? string.Empty : Descriptor.IdentityType,
                (Descriptor == null) ? string.Empty : Descriptor.Identifier,
                DisplayName);
        }

        private bool m_hasModifiedProperties;
    }

    internal class KeyValueOfStringString
    {
        public string Key { get; set; }

        public string Value { get; set; }

        internal static KeyValueOfStringString FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            KeyValueOfStringString obj = new KeyValueOfStringString();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            bool empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
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
                        case "Key":
                            obj.Key = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "Value":
                            obj.Value = XmlUtility.StringFromXmlElement(reader);
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
    }
}
