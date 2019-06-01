using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Account
{
    //NOTE: On data contract types, make all setters
    //for properties that are intended to be read-only on the client
    //side "internal" and give internal access (InternalsVisibleTo) to
    //*only* the assembly that contains the Plaform (i.e. real) implementation
    //of the service.
    //Properties that are updateable or used to create instances of the resource
    //should be public, read\write

    //For this type, there are properties, set and owned by the server (internal set)
    //properties that are updatable by any client (public set)
    //and a few properties that are changable, but should not be set by 
    //any old client -- so these are internal set as well.
    //This class comes down from the server on Get and Create, it is also
    //sent up from the client on Update
    //the only readonly property that can (and must) be set by the
    //client on update is the AccountId
    //The update operation is a PATCH, therefore all properties
    //except AccountId are marked as "IsRequired(false)". They are
    //also marked as EmitDefaultValue(false) to reduce payload size
    //Gets and Creates will fill in all of the set properties
    //Update only needs to set the AccountId and anything it wants to change
    //Client can't, obviously, set readonly properties, so these will
    //always be absent on Update
    [DataContract]
    public sealed class Account
    {
        private Account()
        {
        }

        //Used by client for Update operation
        public Account(Guid accountId) : this()
        {
            AccountId = accountId;
            Properties = new PropertiesCollection();
        }

        /// <summary>
        /// Identifier for an Account
        /// </summary>
        [DataMember]
        public Guid AccountId { get; internal set; }

        /// <summary>
        /// Namespace for an account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid NamespaceId { get; set; }

        /// <summary>
        /// Uri for an account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri AccountUri { get; set; }

        /// <summary>
        /// Name for an account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String AccountName { get; set; }

        /// <summary>
        /// Organization that created the account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String OrganizationName { get; set; }

        /// <summary>
        /// Type of account: Personal, Organization
        /// </summary>
        /// Emit default value because it is an enum...
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public AccountType AccountType { get; set; }

        /// <summary>
        /// Owner of account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid AccountOwner { get; set; }

        /// <summary>
        /// Who created the account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Date account was created
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime CreatedDate { get; internal set; }

        /// <summary>
        /// Current account status
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public AccountStatus AccountStatus { get; set; }

        /// <summary>
        /// Reason for current status
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String StatusReason { get; internal set; }

        /// <summary>
        /// Identity of last person to update the account
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid LastUpdatedBy { get; internal set; }

        /// <summary>
        /// Current revision of the account
        /// </summary>
        [IgnoreDataMember]
        public byte[] Revision { get; internal set; }

        /// <summary>
        /// Extended properties
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public PropertiesCollection Properties
        {
            get; private set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool HasMoved => NewCollectionId != Guid.Empty;

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid NewCollectionId => NamespaceId;

        /// <summary>
        /// Date account was last updated
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastUpdatedDate { get; internal set; }

        /// <summary>
        /// Property accessor. Will throw if not found.
        /// </summary>
        public Object GetProperty(String name)
        {
            return Properties == null ? null : Properties[name];
        }

        /// <summary>
        /// Property accessor. value will be null if not found.
        /// </summary>
        public Boolean TryGetProperty(String name, out Object value)
        {
            value = null;
            return Properties == null ? false : Properties.TryGetValue(name, out value);
        }

        public Account Clone()
        {
            Account a = new Account(AccountId);

            a.NamespaceId = NamespaceId;
            a.AccountUri = AccountUri;
            a.AccountName = AccountName;
            a.OrganizationName = OrganizationName;
            a.AccountType = AccountType;
            a.AccountOwner = AccountOwner;
            a.CreatedBy = CreatedBy;
            a.CreatedDate = CreatedDate;
            a.AccountStatus = AccountStatus;
            a.StatusReason = StatusReason;
            a.LastUpdatedBy = LastUpdatedBy;
            a.LastUpdatedDate = LastUpdatedDate;
            a.Revision = Revision;

            if (Properties != null)
            {
                foreach (var prop in Properties)
                {
                    a.Properties[prop.Key] = prop.Value;
                }
            }

            return a;
        }

        public override String ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Account {0} (Organization: {1}; Type: {2}; Owner: {3}; AccountStatus: {4}; CreatedBy: {5})",
                AccountId,
                OrganizationName,
                AccountType,
                AccountOwner,
                AccountStatus,
                CreatedBy);
        }
    }

    /// <summary>
    /// IEqualityComparer for Account
    /// </summary>
    public class AccountEqualityComparer : IEqualityComparer<Account>
    {
        bool IEqualityComparer<Account>.Equals(Account x, Account y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (!x.AccountId.Equals(y.AccountId))
            {
                return false;
            }

            if (!string.Equals(x.AccountName, y.AccountName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        int IEqualityComparer<Account>.GetHashCode(Account obj)
        {
            return obj.AccountId.GetHashCode() ^ obj.AccountName.GetHashCode();
        }

        public static AccountEqualityComparer Instance { get; } = new AccountEqualityComparer();
    }
}
