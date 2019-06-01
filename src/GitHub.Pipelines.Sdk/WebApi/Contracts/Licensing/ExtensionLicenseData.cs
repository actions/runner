using Microsoft.VisualStudio.Services.Licensing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.WebApi.Contracts.Licensing
{
    [DataContract]
    public class ExtensionLicenseData
    {
        [DataMember]
        public string ExtensionId { get; set; }

        [DataMember]
        public VisualStudioOnlineServiceLevel MinimumRequiredAccessLevel { get; set; }

        [DataMember]
        public bool IsFree { get; set; }

        [DataMember]
        public DateTime CreatedDate { get; set; }

        [DataMember]
        public DateTime UpdatedDate { get; set; }
    }
}