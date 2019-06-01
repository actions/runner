using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Representation of ExternalId we send to CVS and use for for tracing and validation.
    /// NOTE: CVS limits the size of this field to 2048 characters.
    /// </summary>
    [DataContract, ClientIgnore]
    public class ContentValidationExternalId
    {
        [JsonConstructor]
        public ContentValidationExternalId()
        {
        }

        [DataMember(Order = 1)]
        public string FileName { get; set; }

        [DataMember(Order = 2)]
        public string Uri { get; set; }

        [DataMember(Order = 3)]
        public Guid E2EID { get; set; }

        [DataMember(Order = 4)]
        public string Token { get; set; }
    }
}
