using System.Runtime.Serialization;

namespace GitHub.Services.Licensing
{
    [DataContract]
    public class ClientRightsContainer
    {
        [DataMember]
        public byte[] CertificateBytes { get; set; }

        [DataMember]
        public string Token { get; set; }
    }
}
