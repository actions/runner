using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.VisualStudio.Services.Directories.Telemetry;

namespace Microsoft.VisualStudio.Services.Directories
{
    [DataContract]
    public class DirectoryEntityDescriptor : IDirectoryEntityDescriptor
    {
        public string EntityType => entityType ?? DirectoryEntityType.Any;

        public string OriginDirectory => originDirectory ?? DirectoryName.SourceDirectory;

        public string EntityOrigin => entityOrigin;

        public string OriginId => originId;

        public string LocalDirectory => DirectoryName.VisualStudioDirectory;

        public string LocalId => localId;

        public object this[string propertyName]
        {
            get { return Properties.GetValueOrDefault(propertyName); }
            private set { Properties[propertyName] = value; }
        }
        
        public string PrincipalName
        {
            get { return this[DirectoryEntityProperty.PrincipalName] as string; }
            private set { this[DirectoryEntityProperty.PrincipalName] = value; }
        }
        
        public string DisplayName
        {
            get { return this[DirectoryEntityProperty.DisplayName] as string; }
            private set { this[DirectoryEntityProperty.DisplayName] = value; }
        }

        public DirectoryEntityDescriptor(
            string entityType = null,
            string originDirectory = null,
            string originId = null,
            string localId = null,
            string principalName = null,
            string displayName = null,
            IReadOnlyDictionary<string, object> properties = null,
            DirectoryEntityDescriptor baseEntity = null)
        {
            var hasProperties = properties != null && properties.Count > 0;

            if (hasProperties)
            {
                properties.CheckForConflict(DirectoryEntityProperty.PrincipalName, principalName, nameof(principalName), nameof(properties));
                properties.CheckForConflict(DirectoryEntityProperty.DisplayName, displayName, nameof(displayName), nameof(properties));
            }

            if (baseEntity == null)
            {
                this.entityType = entityType;
                this.originDirectory = originDirectory;
                this.originId = originId;
                this.localId = localId;
            }
            else
            {
                this.entityType = entityType ?? baseEntity.EntityType;
                this.originDirectory = originDirectory ?? baseEntity.OriginDirectory;
                this.originId = originId ?? baseEntity.OriginId;
                this.localId = localId ?? baseEntity.LocalId;

                this.properties.SetRangeIfRangeNotNullOrEmpty(baseEntity.Properties);
            }

            if (hasProperties)
            {
                // Extract entity origin value from properties if exists
                if (properties.ContainsKey(DirectoryEntityProperty.EntityOrigin))
                {
                    entityOrigin = (string)properties[DirectoryEntityProperty.EntityOrigin];
                }

                this.properties.Value.SetRangeIfRangeNotNull(properties);
            }

            this.properties.SetIfNotNull(DirectoryEntityProperty.PrincipalName, principalName);
            this.properties.SetIfNotNull(DirectoryEntityProperty.DisplayName, displayName);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        #region Internals

        [DataMember]
        private readonly string entityType;
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        private readonly string entityOrigin;
        [DataMember]
        private readonly string originDirectory;
        [DataMember]
        private readonly string localId;
        [DataMember]
        private readonly string originId;

        private IDictionary<string, object> Properties => properties.Value;

        private Lazy<IDictionary<string, object>> properties = new Lazy<IDictionary<string, object>>(() => new Dictionary<string, object>());

        [JsonConstructor]
        private DirectoryEntityDescriptor(
            string entityType,
            string originDirectory,
            string originId,
            string localId,
            string principalName,
            string displayName,
            string mail,
            DirectoryPermissionsEntry[] localPermissions,
            string invitationMethod,
            bool? allowIntroductionOfMembersNotPreviouslyInScope,
            bool? createIfNotFound,
            bool? includeDeploymentLevelCreation,
            string homeDirectory,
            Guid? masterId,
            Guid? userId,
            Guid? tenantId,
            SubjectDescriptor subjectDescriptor,
            string entityOrigin,
            string puid)
            : this(entityType, originDirectory, originId, localId, principalName, displayName)
        {
            if (entityOrigin != null)
            {
                this.entityOrigin = entityOrigin;
            }

            properties.SetIfNotNull(DirectoryEntityProperty.Mail, mail);
            properties.SetIfNotNull(DirectoryEntityProperty.LocalPermissions, localPermissions);
            properties.SetIfNotNull(DirectoryEntityProperty.Puid, puid);
            properties.SetIfNotNull(DirectoryEntityProperty.TenantId, tenantId);
            properties.SetIfNotNull(DirectoryEntityProperty.SubjectDescriptor, subjectDescriptor);
            properties.SetIfNotNull(DirectoryEntityTelemetryProperty.InvitationMethod, invitationMethod);
            properties.SetIfNotNull(DirectoryEntityMaterializationProperty.AllowIntroductionOfMembersNotPreviouslyInScope, allowIntroductionOfMembersNotPreviouslyInScope);
            properties.SetIfNotNull("CreateIfNotFound", createIfNotFound);
            properties.SetIfNotNull("IncludeDeploymentLevelCreation", includeDeploymentLevelCreation);
            properties.SetIfNotNull("HomeDirectory", homeDirectory);
            properties.SetIfNotNull("MasterId", masterId);
            properties.SetIfNotNull("UserId", userId);
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
