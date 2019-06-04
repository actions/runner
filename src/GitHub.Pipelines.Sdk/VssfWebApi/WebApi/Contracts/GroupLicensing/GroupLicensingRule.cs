using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.GroupLicensingRule
{
    /// <summary>
    /// Represents a GroupLicensingRule
    /// </summary>
    [DataContract]
    public class GroupLicensingRule : IEquatable<GroupLicensingRule>
    {
        /// <summary>
        /// SubjectDescriptor for the rule
        /// </summary>
        [DataMember]
        public SubjectDescriptor SubjectDescriptor { get; set; }

        /// <summary>
        /// License Rule
        /// </summary>
        [DataMember]
        public LicenseRule LicenseRule { get; set; }

        /// <summary>
        /// Extension Rules
        /// </summary>
        [DataMember]
        public IEnumerable<ExtensionRule> ExtensionRules { get; set; }

        /// <summary>
        /// The overall status of the group rule, considering the license status and all the extension statuses
        /// </summary>
        public GroupLicensingRuleStatus Status => ExtensionRules
            .Select(x => x.Status)
            .Union(new[] {LicenseRule.Status})
            .HighestSeverity();

        public GroupLicensingRule()
        {
            ExtensionRules = new List<ExtensionRule>();
        }

        public GroupLicensingRule Clone()
        {
            return new GroupLicensingRule
            {
                SubjectDescriptor = this.SubjectDescriptor,
                LicenseRule = new LicenseRule(this.LicenseRule?.License),
                ExtensionRules = ExtensionRules?.Select(x => new ExtensionRule(x.ExtensionId)) ?? new List<ExtensionRule>(),
            };
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GroupLicensingRule);
        }

        public bool Equals(GroupLicensingRule other)
        {
            return other != null &&
            this.SubjectDescriptor.Equals(other.SubjectDescriptor) &&
            this.LicenseRule.Equals(other.LicenseRule) &&
            this.ExtensionRules.OrderBy(e => e.ExtensionId).SequenceEqual(other.ExtensionRules.OrderBy(e => e.ExtensionId));
        }

        public override int GetHashCode()
        {
            return this.SubjectDescriptor.GetHashCode();
        }

        public static bool Equals(GroupLicensingRule left, GroupLicensingRule right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            else if (object.ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator ==(GroupLicensingRule left, GroupLicensingRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GroupLicensingRule left, GroupLicensingRule right)
        {
            return !Equals(left, right);
        }
    }
}
