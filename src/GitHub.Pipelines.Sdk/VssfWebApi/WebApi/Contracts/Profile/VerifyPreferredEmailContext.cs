using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [DataContract]
    public class VerifyPreferredEmailContext
    {
        [DataMember(IsRequired = true)]
        public Guid Id { get; set; }

        [DataMember(IsRequired = true)]
        public string HashCode { get; set; }

        [DataMember(IsRequired = true)]
        public string EmailAddress { get; set; }
    }
}
