using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Users
{
    [DataContract]
    public class UserAttributes
    {
        /// <summary>
        /// Collection of attributes
        /// </summary>
        [DataMember(IsRequired = true)]
        public IList<UserAttribute> Attributes { get; set; }

        /// <summary>
        /// Opaque string to get the next chunk of results
        /// Server would return non-null string here if there is more data
        /// Client will need then to pass it to the server to get more results
        /// </summary>
        [DataMember(IsRequired = false)]
        public String ContinuationToken { get; set; }
    }
}
