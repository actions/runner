using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// Do not attempt to use this type to create a new group. This
    /// type does not contain sufficient fields to create a new group.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(GraphGroupCreationContextJsonConverter))]
    public abstract class GraphGroupCreationContext
    {
        /// <summary>
        /// Optional: If provided, we will use this identifier for the storage key of the created group
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid StorageKey { get; set; }
    }

    /// <summary>
    /// Use this type to create a new group using the OriginID as a reference to an existing group from an external
    /// AD or AAD backed provider. This is the subset of GraphGroup fields required for creation of
    /// a group for the AD and AAD use case.
    /// </summary>
    [DataContract]
    public class GraphGroupOriginIdCreationContext : GraphGroupCreationContext
    {
        /// <summary>
        /// This should be the object id or sid of the group from the source AD or AAD provider.
        /// Example: d47d025a-ce2f-4a79-8618-e8862ade30dd
        /// Team Services will communicate with the source provider to fill all other fields on creation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string OriginId { get; set; }
    }

    /// <summary>
    /// Use this type to create a new group using the mail address as a reference to an existing group from an external
    /// AD or AAD backed provider. This is the subset of GraphGroup fields required for creation of
    /// a group for the AAD and AD use case.
    /// </summary>
    [DataContract]
    public class GraphGroupMailAddressCreationContext : GraphGroupCreationContext
    {
        /// <summary>
        /// This should be the mail address or the group in the source AD or AAD provider.
        /// Example: jamal@contoso.com
        /// Team Services will communicate with the source provider to fill all other fields on creation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string MailAddress { get; set; }
    }

    /// <summary>
    /// Use this type to create a new Vsts group that is not backed by an external provider.
    /// </summary>
    [DataContract]
    public class GraphGroupVstsCreationContext : GraphGroupCreationContext
    {
        /// <summary>
        /// Used by VSTS groups; if set this will be the group DisplayName, otherwise ignored
        /// </summary>
        [DataMember(IsRequired = true)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Used by VSTS groups; if set this will be the group description, otherwise ignored
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Internal use only. An optional sid to use for group creation.
        /// </summary>
        public SubjectDescriptor Descriptor { get; set; }


        [DataMember(Name = "Descriptor", IsRequired = false, EmitDefaultValue = false)]
        private string DescriptorString
        {
            get { return Descriptor.ToString(); }
            set { Descriptor = SubjectDescriptor.FromString(value); }
        }
        /// <summary>
        /// For internal use only in back compat scenarios.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool CrossProject { get; set; }

        /// <summary>
        /// For internal use only in back compat scenarios.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool RestrictedVisibility { get; set; }

        /// <summary>
        /// For internal use only in back compat scenarios.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string SpecialGroupType { get; set; }
    }
}
