using System;
using System.Runtime.Serialization;

namespace GitHub.Services.GroupLicensingRule
{
    /// <summary>
    /// Represents an Extension Rule
    /// </summary>
    [DataContract]
    public class ExtensionRule : IEquatable<ExtensionRule>
    {
        /// <summary>
        /// Extension Id
        /// </summary>
        [DataMember]
        public string ExtensionId { get; set; }

        /// <summary>
        /// Status of the group rule (applied, missing licenses, etc)
        /// </summary>
        [DataMember]
        public GroupLicensingRuleStatus Status { get; set; }

        public ExtensionRule()
        {
            
        }

        public ExtensionRule(string extensionId)
        {
            ExtensionId = extensionId;
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ExtensionRule);
        }

        public bool Equals(ExtensionRule other)
        {
            return other != null && this.ExtensionId.Equals(other.ExtensionId);
        }

        public override int GetHashCode()
        {
            return this.ExtensionId.GetHashCode();
        }

        public static bool Equals(ExtensionRule left, ExtensionRule right)
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

        public static bool operator ==(ExtensionRule left, ExtensionRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtensionRule left, ExtensionRule right)
        {
            return !Equals(left, right);
        }

        #endregion IEquatable
    }
}
