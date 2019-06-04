using System;
using System.Runtime.Serialization;
using GitHub.Services.Licensing;

namespace GitHub.Services.GroupLicensingRule
{
    /// <summary>
    /// Represents a License Rule
    /// </summary>
    [DataContract]
    public class LicenseRule : IEquatable<LicenseRule>
    {
        /// <summary>
        /// License
        /// </summary>
        [DataMember]
        private License license { get; set; }

        /// <summary>
        /// Status of the group rule (applied, missing licenses, etc)
        /// </summary>
        [DataMember]
        public GroupLicensingRuleStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="License"/> for the LicenseRule
        /// </summary>
        public License License
        {
            get { return license ?? License.None; }
            set { this.license = value; }
        }

        /// <summary>
        /// The last time the rule was executed (regardless of whether any changes were made)
        /// </summary>
        [DataMember]
        public DateTimeOffset? LastExecuted { get; set; }

        public LicenseRule()
        {
            
        }

        public LicenseRule(License license)
        {
            this.license = license;
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return this.Equals(obj as LicenseRule);
        }

        public bool Equals(LicenseRule other)
        {
            return other != null && this.License.Equals(other.license);
        }

        public override int GetHashCode()
        {
            return this.License.GetHashCode();
        }

        public static bool Equals(LicenseRule left, LicenseRule right)
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

        public static bool operator ==(LicenseRule left, LicenseRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LicenseRule left, LicenseRule right)
        {
            return !Equals(left, right);
        }

        #endregion IEquatable
    }
}
