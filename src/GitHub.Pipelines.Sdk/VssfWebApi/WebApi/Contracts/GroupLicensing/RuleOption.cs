using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.GroupLicensingRule
{
    [DataContract]
    public enum RuleOption
    {
        ApplyGroupRule,

        TestApplyGroupRule
    }
}
