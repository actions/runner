using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile.Model
{
    [DataContract]
    public class RemoteProfile
    {
        [DataMember]
        public string DisplayName;

        /// <summary>
        /// Primary contact email from from MSA/AAD
        /// </summary>
        [DataMember]
        public string EmailAddress;

        [DataMember]
        public byte[] Avatar;

        [DataMember]
        public string CountryCode;
    }
}