using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalGitUser : IAdditionalProperties
    {
        /// <summary>
        /// Name or login of the user depending on the source of the data.
        /// </summary>
        [DataMember]
        public String Name;

        /// <summary>
        /// Email address of the user.
        /// </summary>
        [DataMember]
        public String Email;

        /// <summary>
        /// URL of the user's avatar image.
        /// </summary>
        [DataMember]
        public String AvatarUrl;

        /// <summary>
        /// Bucket for storing external data source related properties
        /// </summary>
        [DataMember]
        public IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
