using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [DataContract]
    public class ProfileTermsOfService
    {
        [DataMember]
        public Guid Id { get; internal set; }

        [DataMember]
        public string TermsOfServiceUrl { get; internal set; }

        [DataMember]
        public int Version { get; internal set; }

        [DataMember]
        public DateTime ActivatedDate { get; internal set; }

        [DataMember]
        public DateTime LastModified { get; internal set; }
    }
}
