using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Organization.Client
{
    [DataContract]
    public sealed class Logo
    {
        /// <summary>
        /// The image for the logo represented as a byte array
        /// </summary>
        [DataMember]
        public byte[] Image { get; set; }
    }
}
