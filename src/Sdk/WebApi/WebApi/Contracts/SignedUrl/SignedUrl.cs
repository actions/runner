using System;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// A signed url allowing limited-time anonymous access to private resources.
    /// </summary>
    [DataContract]
    public class SignedUrl
    {
        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public DateTime SignatureExpires { get; set; }
    }
}
