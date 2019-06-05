using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace GitHub.Services.Account
{
    [DataContract]
    public sealed class AccountUser
    {
        private AccountUser()
        {
        }

        //Used by client for Update operation
        public AccountUser(Guid accountId, Guid userId) : this()
        {
            AccountId = accountId;
            UserId = userId;
        }

        /// <summary>
        /// Identifier for an Account
        /// </summary>
        [DataMember]
        public Guid AccountId { get;  set; }

        /// <summary>
        /// The user identity to be associated with the account
        /// </summary>
        [DataMember]
        public Guid UserId { get; set; }

        /// <summary>
        /// Date account was created
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public DateTimeOffset CreationDate { get;  set; }

        /// <summary>
        /// Current account status
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public AccountUserStatus UserStatus { get; set; }

        /// <summary>
        /// What is the license for this user MSDN, VSPro, etc.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public AccountLicenseSource LicenseSource { get; set; }

        /// <summary>
        /// Date account was last updated
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public DateTimeOffset LastUpdated { get; set; }

        /// <summary>
        /// What are the users service rights
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public AccountServiceRights ServiceRights {get; set; }

        public AccountUser Clone()
        {
            AccountUser a = new AccountUser(AccountId, UserId);

            a.AccountId = AccountId;
            a.UserId = UserId;
            a.CreationDate = CreationDate;
            a.LastUpdated = LastUpdated;
            a.LicenseSource = LicenseSource;
            a.UserStatus = UserStatus;
            a.ServiceRights = ServiceRights;
            return a;
        }
        
        public override String ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "AccountUser: AccountId {0}, UserId {1} (UserStatus: {2}; LicenseSource: {3}; ServiceRights: {4})",
                AccountId,
                UserId, 
                UserStatus,
                LicenseSource,
                ServiceRights);
        }
   }

    [DataContract]
    public class AccountLicenseInfo
    {
        private AccountLicenseInfo()
        {
        }

        public AccountLicenseInfo(string licenseName, int provisioned, int consumed) : this()
        {
            LicenseName = licenseName;
            InUseCount = provisioned;
            ConsumedCount = consumed;
        }

        [DataMember]
        public string LicenseName { get; set; }

        [DataMember]
        public int InUseCount { get; set; }

        [DataMember]
        public int ConsumedCount { get; set; }

        public AccountLicenseInfo Clone()
        {
            AccountLicenseInfo ali = new AccountLicenseInfo();
            ali.ConsumedCount = ConsumedCount;
            ali.LicenseName = LicenseName;
            ali.InUseCount = InUseCount;

            return ali;
        }

        public override String ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "AccountLicenseInfo: LicenseName {0}, ProvisionedCount {1}, ConsumedCount {2}",
                LicenseName, InUseCount, ConsumedCount);
        }
    }

}
