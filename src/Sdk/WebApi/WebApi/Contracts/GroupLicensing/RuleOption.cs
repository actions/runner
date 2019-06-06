using System.Runtime.Serialization;

namespace GitHub.Services.GroupLicensingRule
{
    [DataContract]
    public enum RuleOption
    {
        ApplyGroupRule,

        TestApplyGroupRule
    }
}
