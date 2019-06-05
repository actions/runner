using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Organization.Client
{
    [DataContract]
    public sealed class OrganizationMigrationBlob
    {
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string BlobAsJson { get; set; }
    }
}
