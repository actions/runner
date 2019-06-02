using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{

    [DataContract]
    public class UserTermsOfService
    {
        [DataMember]
        public int CurrentAcceptedTermsOfService { get; set; }

        [DataMember]
        public DateTimeOffset CurrentAcceptedTermsOfServiceDate { get; set; }

        [DataMember]
        public ProfileTermsOfService LatestTermsOfService { get; set; }
    }
}
