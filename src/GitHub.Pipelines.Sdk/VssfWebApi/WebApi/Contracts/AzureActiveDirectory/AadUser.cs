using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Services.Aad
{
    /// <summary>
    /// Immutable data transfer object for AAD user details.
    /// </summary>
    [DataContract]
    public class AadUser : AadObject
    {        
        [DataMember] private bool accountEnabled;
        [DataMember] private string mail;
        [DataMember] private IEnumerable<string> otherMails;
        [DataMember] private string mailNickname;
        [DataMember] private string userPrincipalName;
        [DataMember] private string signInAddress;
        [DataMember] private bool hasThumbnailPhoto;
        [DataMember] private string jobTitle;
        [DataMember] private string department;
        [DataMember] private string physicalDeliveryOfficeName;
        [DataMember] private AadUser manager;
		[DataMember] private IEnumerable<AadUser> directReports;
		[DataMember] private string userType;
        [DataMember] private string userState;
        [DataMember] private string surname;
        [DataMember] private string onPremisesSecurityIdentifier;
        [DataMember] private string immutableId;
        [DataMember] private string telephoneNumber;
        [DataMember] private string country;
        [DataMember] private string usageLocation;

		protected AadUser() { }

        private AadUser(Guid objectId, string displayName, bool accountEnabled, string mail, IEnumerable<string> otherMails, string userPrincipalName, string signInAddress,
            bool hasThumbnailPhoto, string jobTitle, string department, string physicalDeliveryOfficeName, string mailNickname, AadUser manager, IEnumerable<AadUser> directReports, string userType, string userState, string surname, string onPremisesSecurityIdentifier, string immutableId, string telephoneNumber,
            string country, string usageLocation)
            : base(objectId, displayName)
        {
            this.accountEnabled = accountEnabled;
            this.mail = mail;
            this.otherMails = otherMails;
            this.mailNickname = mailNickname;
            this.userPrincipalName = userPrincipalName;
            this.signInAddress = signInAddress;
            this.hasThumbnailPhoto = hasThumbnailPhoto;
            this.jobTitle = jobTitle;
            this.department = department;
            this.physicalDeliveryOfficeName = physicalDeliveryOfficeName;
            this.manager = manager;
			this.directReports = directReports;
            this.userType = userType;
            this.userState = userState;
            this.surname = surname;
            this.onPremisesSecurityIdentifier = onPremisesSecurityIdentifier;
            this.immutableId = immutableId;
            this.telephoneNumber = telephoneNumber;
            this.country = country;
            this.usageLocation = usageLocation;
		}

        public bool AccountEnabled
        {
            get { return accountEnabled; }
            set { accountEnabled = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string Mail
        {
            get { return mail; }
            set { mail = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public IEnumerable<string> OtherMails
        {
            get { return otherMails; }
            set { otherMails = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string MailNickname
        {
            get { return mailNickname; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string UserPrincipalName
        {
            get { return userPrincipalName; }
            set { userPrincipalName = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string SignInAddress
        {
            get { return signInAddress; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public bool HasThumbnailPhoto
        {
            get { return hasThumbnailPhoto; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string JobTitle
        {
            get { return jobTitle; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string Department
        {
            get { return department; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string PhysicalDeliveryOfficeName
        {
            get { return physicalDeliveryOfficeName; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public AadUser Manager
        {
            get { return manager; }
        }

		/// <summary>
		/// This could be null.
		/// </summary>
		public IEnumerable<AadUser> DirectReports
		{
			get { return directReports; }
		}

        /// <summary>
        /// This could be null.
        /// </summary>
        public string UserType
        {
            get { return userType; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string UserState
        {
            get { return userState; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string Surname
        {
            get { return surname; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string OnPremisesSecurityIdentifier
        {
            get { return onPremisesSecurityIdentifier; }
        }

        public string ImmutableId
        {
            get { return immutableId; }
        }

		/// <summary>
		/// This could be null.
		/// </summary>
        public string TelephoneNumber
		{
            get { return telephoneNumber; }
            set { telephoneNumber = value; }
		}

        /// <summary>
        /// This could be null.
        /// </summary>
        public string Country
        {
            get { return country; }
            set { country = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string UsageLocation
        {
            get { return usageLocation; }
            set { usageLocation = value; }
        }
		
		/// <summary>
		/// Creates immutable <see cref="AadUser"/> objects.
		/// </summary>
		public class Factory
        {
            /// <summary>
            /// Creates an <see cref="AadUser"/> object.
            /// </summary>
            public AadUser Create()
            {
                var otherMails = OtherMails;
                if (otherMails != null)
                {
                    otherMails = otherMails.ToArray();
                }
                var directReports = DirectReports;
                if (directReports != null)
                {
                    directReports = directReports.ToArray();
                }
                return new AadUser(ObjectId, DisplayName, AccountEnabled, Mail, otherMails, UserPrincipalName, SignInAddress, HasThumbnailPhoto, JobTitle, Department, PhysicalDeliveryOfficeName, MailNickname, Manager, directReports, UserType, UserState, Surname, OnPremisesSecurityIdentifier, ImmutableId, TelephoneNumber, Country, UsageLocation);
            }

            public Guid ObjectId { get; set; }
            public string DisplayName { get; set; }
            public bool AccountEnabled { get; set; }
            public string Mail { get; set; }
            public IEnumerable<string> OtherMails { get; set; }
            public string MailNickname { get; set; }
            public string UserPrincipalName { get; set; }
            public string SignInAddress { get; set; }
            public bool HasThumbnailPhoto { get; set; }
            public string JobTitle { get; set; }
            public string Department { get; set; }
            public string PhysicalDeliveryOfficeName { get; set; }
            public AadUser Manager { get; set; }
			public IEnumerable<AadUser> DirectReports { get; set; }
            public string UserType { get; set; }
            public string UserState { get; set; }
            public string Surname { get; set; }
            public string OnPremisesSecurityIdentifier { get; set; }
            public string ImmutableId { get; set; }
            public string TelephoneNumber { get; set; }
            public string Country { get; set; }
            public string UsageLocation { get; set; }
        }
    }
}
