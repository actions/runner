using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.Graph.Client;
using GitHub.Services.WebApi.Xml;
using Newtonsoft.Json;

namespace GitHub.Services.WebApi
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [XmlSerializableDataContract(EnableCamelCaseNameCompat = true)]
    public class IdentityRef : GraphSubjectBase, ISecuredObject
    {
        // The following "new" properties are inherited from the base class,
        // but are reimplemented with public setters for back compat.

        public new SubjectDescriptor Descriptor
        {
            get { return base.Descriptor; }
            set { base.Descriptor = value; }
        }

        public new string DisplayName
        {
            get { return base.DisplayName; }
            set { base.DisplayName = value; }
        }

        public new string Url
        {
            get { return base.Url; }
            set { base.Url = value; }
        }

        public new ReferenceLinks Links
        {
            get { return base.Links; }
            set { base.Links = value; }
        }

        [DataMember(Name = "id")]
        [JsonProperty(PropertyName = "id")]
        public String Id { get; set; }

        // Deprecated. See https://dev.azure.com/mseng/VSOnline/_wiki/wikis/VSOnline.wiki?wikiVersion=GBwikiMaster&pagePath=%2FTeam%20Pages%2FPipelines%2FPublic%20projects&anchor=obsolete-identity-fields
        /// <summary>
        /// Deprecated - use Domain+PrincipalName instead
        /// </summary>
        [DataMember(Name = "uniqueName", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "uniqueName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(DefaultValueOnPublicAccessJsonConverter<String>))]
        public String UniqueName { get; set; }

        /// <summary>
        /// Deprecated - Can be retrieved by querying the Graph user referenced in the "self" entry of the IdentityRef "_links" dictionary
        /// </summary>
        [DataMember(Name = "directoryAlias", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "directoryAlias", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(DefaultValueOnPublicAccessJsonConverter<String>))]
        public String DirectoryAlias { get; set; }

        /// <summary>
        /// Deprecated - not in use in most preexisting implementations of ToIdentityRef
        /// </summary>
        [DataMember(Name = "profileUrl", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "profileUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(DefaultValueOnPublicAccessJsonConverter<String>))]
        public String ProfileUrl { get; set; }

        /// <summary>
        /// Deprecated - Available in the "avatar" entry of the IdentityRef "_links" dictionary
        /// </summary>
        [DataMember(Name = "imageUrl", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "imageUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(DefaultValueOnPublicAccessJsonConverter<String>))]
        public String ImageUrl { get; set; }

        /// <summary>
        /// Deprecated - Can be inferred from the subject type of the descriptor (Descriptor.IsGroupType)
        /// </summary>
        [DataMember(Name = "isContainer", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "isContainer", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Boolean IsContainer { get; set; }

        /// <summary>
        /// Deprecated - Can be inferred from the subject type of the descriptor (Descriptor.IsAadUserType/Descriptor.IsAadGroupType)
        /// </summary>
        [DataMember(Name = "isAadIdentity", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "isAadIdentity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Boolean IsAadIdentity { get; set; }

        /// <summary>
        /// Deprecated - Can be retrieved by querying the Graph membership state referenced in the "membershipState" entry of the GraphUser "_links" dictionary
        /// </summary>
        [DataMember(Name = "inactive", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "inactive", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Boolean Inactive { get; set; }

        [DataMember(Name = "isDeletedInOrigin", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "isDeletedInOrigin", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Boolean IsDeletedInOrigin { get; set; }

        /// <summary>
        /// This property is for xml compat only.
        /// </summary>
        [DataMember(Name = "displayName", EmitDefaultValue = false)]
        [JsonIgnore, Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public string DisplayNameForXmlSerialization { get => base.DisplayName; set => base.DisplayName = value; }

        /// <summary>
        /// This property is for xml compat only.
        /// </summary>
        [DataMember(Name = "url", EmitDefaultValue = false)]
        [JsonIgnore, Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public string UrlForXmlSerialization { get => base.Url; set => base.Url = value; }

        Guid ISecuredObject.NamespaceId => GraphSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => GraphSecurityConstants.ReadByPublicIdentifier;

        string ISecuredObject.GetToken() => GraphSecurityConstants.RefsToken;
    }
}
