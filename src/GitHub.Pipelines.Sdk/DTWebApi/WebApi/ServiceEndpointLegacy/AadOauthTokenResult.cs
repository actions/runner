using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class AadOauthTokenResult
    {
        [DataMember]
        public String AccessToken { get; set; }

        [DataMember]
        public String RefreshTokenCache { get; set; }
    }
}