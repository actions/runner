using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Organization.Client
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