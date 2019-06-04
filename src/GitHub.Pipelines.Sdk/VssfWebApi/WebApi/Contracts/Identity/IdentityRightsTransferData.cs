using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class IdentityRightsTransferData
    {
        [DataMember]
        public Dictionary<String, String> UserPrincipalNameMappings { get; }

        public IdentityRightsTransferData(Dictionary<String, String> userPrincipalNameMappings)
        {
            ArgumentUtility.CheckForNull(userPrincipalNameMappings, nameof(userPrincipalNameMappings));
            this.UserPrincipalNameMappings = userPrincipalNameMappings;
        }
    }
}
