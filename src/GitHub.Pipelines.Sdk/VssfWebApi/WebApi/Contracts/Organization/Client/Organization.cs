using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Organization.Client
{
    [DataContract]
    public sealed class Organization
    {
        /// <summary>
        /// Identifier for an Organization
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public OrganizationType Type { get;  set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public OrganizationStatus Status { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string PreferredRegion { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsActivated { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ICollection<Collection> Collections { get;  set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime DateCreated { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastUpdated { get;  set; }

        // from here down only used during POST create calls
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid CreatorId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid TenantId { get; internal set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Collection PrimaryCollection { get; set; }

        /// <summary>
        /// Extended properties
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public PropertiesCollection Properties { get; set; }

        // only used for creation
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IDictionary<string, object> Data { get; set; }
    }
}
