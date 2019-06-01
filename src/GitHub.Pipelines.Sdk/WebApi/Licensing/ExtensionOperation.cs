using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public enum ExtensionOperation
    {
        [EnumMember]
        Assign = 0,

        [EnumMember]
        Unassign = 1
    }

    public enum ExtensionRightsResultCode
    {
        [EnumMember]
        Normal = 0,

        [EnumMember]
        AllFree = 1,

        [EnumMember]
        FreeExtensionsFree = 2
    }

    public enum ExtensionRightsReasonCode
    {
        [EnumMember]
        Normal = 0,

        [EnumMember]
        FeatureFlagSet = 1,

        [EnumMember]
        NullIdentity = 2,

        [EnumMember]
        ServiceIdentity = 3,

        [EnumMember]
        ErrorCallingService = 4
    }


    public class ExtensionRightsResult
    {
        // Currently this is the Collection Id
        public Guid HostId { get; set; }

        // Once this value is set, only operation for this, is reading.
        // And this structure is handled within licensing code.
        // No need for concurrent collections.
        public HashSet<string> EntitledExtensions { get; set; }

        // This affect how we evaluate results.
        public ExtensionRightsResultCode ResultCode { get; set; }

        // Reasons For sending this result.
        public ExtensionRightsReasonCode ReasonCode { get; set; }

        // More detailed information on reason code.
        public string Reason { get; set; }

        public override string ToString()
        {
            var extensions = this.EntitledExtensions == null ? string.Empty : string.Join("|", this.EntitledExtensions);
            return $"HostId: {this.HostId}; ResultCode: {this.ResultCode}; EntitledExtensions:{extensions}";
        }
    }
}
