using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class AuthorizationHeader
    {
        /// <summary>
        /// Gets or sets the name of authorization header.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the value of authorization header.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Value { get; set; }
    }
}