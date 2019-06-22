using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    // TODO: remove this before dev16 ships. leaving it in for the dev15 cycle to avoid any issues
    [Obsolete("This contract is not used by any product code")]
    [DataContract]
    public class RequestReference
    {
        /// <summary>
        /// Id of the resource
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 Id { get; set; }
        
        /// <summary>
        /// Full http link to the resource
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Url { get; set; }

        /// <summary>
        /// Name of the requestor
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IdentityRef RequestedFor { get; set; }
    }
}
