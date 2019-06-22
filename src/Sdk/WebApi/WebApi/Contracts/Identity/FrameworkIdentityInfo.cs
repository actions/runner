using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class FrameworkIdentityInfo
    {
        [DataMember]
        public FrameworkIdentityType IdentityType { get; set; }

        [DataMember]
        public string Role { get; set; }

        [DataMember]
        public string Identifier { get; set; }

        [DataMember]
        public string DisplayName { get; set; }
    }
}
