using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Security
{
    [DataContract]
    public sealed class SetInheritFlagInfo
    {
        public SetInheritFlagInfo(
            String token,
            Boolean inherit)
        {
            Token = token;
            Inherit = inherit;
        }

        [DataMember]
        public String Token { get; private set; }

        [DataMember]
        public Boolean Inherit { get; private set; }
    }
}
