using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
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
