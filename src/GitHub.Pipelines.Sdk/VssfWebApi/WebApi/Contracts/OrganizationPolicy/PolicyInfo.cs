using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Organization.Client
{
    [DataContract]
    [ClientIncludeModel]
    public sealed class PolicyInfo
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri MoreInfoLink { get; set; }
    }
}
