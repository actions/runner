using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Organization.Client
{
    [DataContract]
    public sealed class Collection
    {
        public Collection()
        {
            Data = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Identifier for a collection under an organization
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// The unqiue name of collection under an organziation
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public CollectionStatus Status { get;  set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid Owner { get;  set; }

        /// <summary>
        /// The tenant id of the AAD tenant backing the enterprise to which the collection belongs
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid TenantId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime DateCreated { get;  set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastUpdated { get;  set; }

        [IgnoreDataMember]
        public int Revision { get;  set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string PreferredRegion { get; set; }

        /// <summary>
        /// Extended properties
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public PropertiesCollection Properties { get; set; }

        // only used for creation
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IDictionary<string, object> Data { get; set;  }
    }	
}