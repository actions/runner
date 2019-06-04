using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitHub.Services.Directories
{
    /// <summary>
    /// Read-write base class.
    /// </summary>
    [DataContract]
    internal abstract class DirectoryEntity : IDirectoryEntity
    {
        [DataMember]
        public string EntityId { get; internal set; }

        [DataMember]
        public string EntityType { get; internal set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string EntityOrigin { get; internal set; }

        [DataMember]
        public string OriginDirectory { get; internal set; }

        [DataMember]
        public string OriginId { get; internal set; }

        [DataMember]
        public string LocalDirectory { get; internal set; }

        [DataMember]
        public string LocalId { get; internal set; }

        public object this[string propertyName]
        {
            get
            {
                object value = null;
                if (Properties != null)
                {
                    Properties.TryGetValue(propertyName, out value);
                }
                return value;
            }

            internal set
            {
                if (Properties == null)
                {
                    Properties = new Dictionary<string, object>();
                }
                Properties[propertyName] = value;
            }
        }
        
        public string PrincipalName
        {
            get { return this[DirectoryEntityProperty.PrincipalName] as string; }
            internal set { this[DirectoryEntityProperty.PrincipalName] = value; }
        }

        public bool? Active
        {
            get { return this[DirectoryEntityProperty.Active] as bool?; }
            internal set { this[DirectoryEntityProperty.Active] = value; }
        }

        public string DisplayName
        {
            get { return this[DirectoryEntityProperty.DisplayName] as string; }
            internal set { this[DirectoryEntityProperty.DisplayName] = value; }
        }

        public SubjectDescriptor? SubjectDescriptor
        {
            get { return this[DirectoryEntityProperty.SubjectDescriptor] as SubjectDescriptor?; }
            internal set { this[DirectoryEntityProperty.SubjectDescriptor] = value; }
        }

        public string ScopeName
        {
            get { return this[DirectoryEntityProperty.ScopeName] as string; }
            internal set { this[DirectoryEntityProperty.ScopeName] = value; }
        }
        
        public string LocalDescriptor
        {
            get { return this[DirectoryEntityProperty.LocalDescriptor]?.ToString(); }
            internal set { this[DirectoryEntityProperty.LocalDescriptor] = value; }
        }

        public override string ToString()
        {
            return string.Format("{0} <{1}>", DisplayName, EntityId);
        }

        public bool Equals(DirectoryEntity other)
        {
            if (other == null) return false;

            return this.EntityId.Equals(other.EntityId);
        }

        public override int GetHashCode()
        {
            return this.EntityId != null ? this.EntityId.GetHashCode() : 0;
        }

        #region Internals

        internal IDictionary<string, object> Properties { get; set; }

        internal DirectoryEntity() { }

        [JsonConstructor]
        protected DirectoryEntity(
            string entityId,
            string entityType,
            string originDirectory,
            string originId,
            string localDirectory,
            string localId,
            string principalName,
            string displayName,
            SubjectDescriptor? subjectDescriptor,
            string scopeName,
            string localDescriptor,
            DirectoryPermissionsEntry[] localPermissions,
            string entityOrigin = null)
        {
            EntityId = entityId;
            EntityType = entityType;
            EntityOrigin = entityOrigin;
            OriginDirectory = originDirectory;
            OriginId = originId;
            LocalDirectory = localDirectory;
            LocalId = localId;
            Properties = new Dictionary<string, object>();
            Properties.SetIfNotNull(DirectoryEntityProperty.PrincipalName, principalName);
            Properties.SetIfNotNull(DirectoryEntityProperty.DisplayName, displayName);
            Properties.SetIfNotNull(DirectoryEntityProperty.SubjectDescriptor, subjectDescriptor);
            Properties.SetIfNotNull(DirectoryEntityProperty.ScopeName, scopeName);
            Properties.SetIfNotNull(DirectoryEntityProperty.LocalDescriptor, localDescriptor);
            Properties.SetIfNotNull(DirectoryEntityProperty.LocalPermissions, localPermissions);
        }

        [JsonExtensionData(ReadData = false, WriteData = true)]
        private IDictionary<string, object> PropertiesToSerializeOut
        {
            get { return Properties.ToDictionary(property => CamelCaseResolver.GetResolvedPropertyName(property.Key), property => property.Value); }
            // JsonExtensionData requires set property even when ReadData is false in json.net v6 and lower
            set { }
        }

        private static readonly CamelCasePropertyNamesContractResolver CamelCaseResolver = new CamelCasePropertyNamesContractResolver();

        #endregion
    }
}
