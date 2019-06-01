using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class ClientCertificate
    {
        /// <summary>
        /// Gets or sets the value of client certificate.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Value { get; set; }
    }
}