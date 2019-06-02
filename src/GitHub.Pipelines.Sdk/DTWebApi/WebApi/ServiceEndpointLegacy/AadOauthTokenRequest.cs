using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class AadOauthTokenRequest
    {
        [DataMember]
        public String Token { get; set; }

        [DataMember]
        public String Resource { get; set; }

        [DataMember]
        public String TenantId { get; set; }

        [DataMember]
        public Boolean Refresh { get; set; }
    }
}