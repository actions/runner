using Microsoft.VisualStudio.Services.Account;
using System;

using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Licensing
{
    /// <summary>
    /// Represents a license granted to a user in an account
    /// </summary>
    [DataContract]
    public class AccountEntitlement : IEquatable<AccountEntitlement>
    {
        [DataMember]
        private License license { get; set; }

        /// <summary>
        /// Gets or sets the id of the account to which the license belongs
        /// </summary>
        /// <remarks>Optional since it was not originally included in the REST contract.</remarks>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid AccountId { get; internal set; }

        /// <summary>
        /// Gets the id of the user to which the license belongs
        /// </summary>
        [DataMember]
        public Guid UserId { get; set; }

        /// <summary>
        /// Identity information of the user to which the license belongs
        /// </summary>
        /// <remarks>Optional because not all clients use this.</remarks>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IdentityRef User { get; internal set; }

        /// <summary>
        /// Gets or sets the <see cref="License"/> for the entitlement
        /// </summary>        
        public License License 
        {
            get { return license ?? License.None; }
            set { this.license = value; }
        }

        /// <summary>
        /// Assignment Source
        /// </summary>
        [DataMember]
        public AssignmentSource AssignmentSource { get; set; }

        /// <summary>
        /// Licensing origin
        /// </summary>
        [DataMember]
        public LicensingOrigin Origin { get; set; }

        /// <summary>
        /// The status of the user in the account
        /// </summary>
        /// <remarks>Serialized as "status" for back compat with previous clients.</remarks>
        [DataMember(Name ="status")]
        public AccountUserStatus UserStatus { get; set; }

        /// <summary>
        /// Gets or sets the date the license was assigned
        /// </summary>
        [DataMember]
        public DateTimeOffset AssignmentDate { get; set; }
        
        /// <summary>
        /// Gets or sets the date of the user last sign-in to this account
        /// </summary>
        [DataMember]
        public DateTimeOffset LastAccessedDate { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the user in this account
        /// </summary>
        [DataMember]
        public DateTimeOffset DateCreated { get; set; }

        /// <summary>
        /// The computed rights of this user in the account.
        /// </summary>
        /// <remarks>Optional it was not originally included in the REST contract.</remarks>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public AccountRights Rights { get; set; }

        public static bool operator ==(AccountEntitlement left, AccountEntitlement right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AccountEntitlement left, AccountEntitlement right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AccountEntitlement);
        }

        public bool Equals(AccountEntitlement other)
        {
            return other != null &&
                   UserId.Equals(other.UserId) &&
                   License.Equals(other.License) &&
                   AssignmentSource == other.AssignmentSource &&
                   Origin == other.Origin &&
                   UserStatus == other.UserStatus &&
                   AssignmentDate.Equals(other.AssignmentDate) &&
                   LastAccessedDate.Equals(other.LastAccessedDate) &&
                   DateCreated.Equals(other.DateCreated);
        }

        public override int GetHashCode()
        {
            var hashCode = -508375918;
            hashCode = hashCode * -1521134295 + EqualityComparer<License>.Default.GetHashCode(license);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(UserId);
            hashCode = hashCode * -1521134295 + EqualityComparer<IdentityRef>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 + EqualityComparer<License>.Default.GetHashCode(License);
            hashCode = hashCode * -1521134295 + AssignmentSource.GetHashCode();
            hashCode = hashCode * -1521134295 + Origin.GetHashCode();
            hashCode = hashCode * -1521134295 + UserStatus.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(AssignmentDate);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(LastAccessedDate);
            hashCode = hashCode * -1521134295 + EqualityComparer<AccountRights>.Default.GetHashCode(Rights);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(DateCreated);
            return hashCode;
        }
    }
}
