using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.Licensing;

namespace Microsoft.VisualStudio.Services.GroupLicensingRule
{
    [DataContract]
    public class ApplicationStatus
    {
        [DataMember]
        public OperationStatus Status { get; set; }

        [DataMember]
        public RuleOption Option { get; set; }

        [DataMember]
        public bool IsTruncated { get; set; }

        [DataMember]
        public ICollection<LicenseApplicationStatus> Licenses { get; set; }

        [DataMember]
        public ICollection<ExtensionApplicationStatus> Extensions { get; set; }

        public ApplicationStatus()
        {
            
        }

        public ApplicationStatus(OperationStatus status, RuleOption option = RuleOption.TestApplyGroupRule)
        {
            Status = status;
            Option = option;
        }
    }

    [DataContract]
    public class LicensingApplicationStatus
    {
        [DataMember]
        public int Assigned { get; set; }

        [DataMember]
        public int InsufficientResources { get; set; }

        [DataMember]
        public int Failed { get; set; }
    }

    [DataContract]
    public class LicenseApplicationStatus : LicensingApplicationStatus
    {
        [DataMember]
        public License License { get; set; }

        [DataMember]
        public AccountUserLicense AccountUserLicense { get; set; }
    }

    [DataContract]
    public class ExtensionApplicationStatus : LicensingApplicationStatus
    {
        [DataMember]
        public string ExtensionId { get; set; }

        [DataMember]
        public int Unassigned { get; set; }

        [DataMember]
        public int Incompatible { get; set; }
    }

}
