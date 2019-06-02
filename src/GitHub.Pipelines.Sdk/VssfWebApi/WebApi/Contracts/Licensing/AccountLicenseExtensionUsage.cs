using Microsoft.VisualStudio.Services.Commerce;
using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
{
    [DataContract]
    public class AccountLicenseExtensionUsage
    {
        [DataMember]
        public string ExtensionName { get; set; }

        [DataMember]
        public string ExtensionId { get; set; }

        [DataMember]
        public int ProvisionedCount { get; set; }

        [DataMember]
        public int IncludedQuantity { get; set; }

        [DataMember]
        public int UsedCount { get; set; }

        [DataMember]
        public int MsdnUsedCount { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public bool IsTrial { get; set; }

        [DataMember(IsRequired = false)]
        public int RemainingTrialDays { get; set; }

        [DataMember]
        public MinimumRequiredServiceLevel MinimumLicenseRequired { get; set; }

        [DataMember(IsRequired = false)]
        public DateTime? TrialExpiryDate { get; set; }
    }
}