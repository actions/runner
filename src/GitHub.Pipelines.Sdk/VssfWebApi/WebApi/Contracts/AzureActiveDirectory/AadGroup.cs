using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Aad
{
    /// <summary>
    /// Immutable data transfer object for AAD group details.
    /// </summary>
    [DataContract]
    public class AadGroup : AadObject
    {
        [DataMember] private string description;
        [DataMember] private string mailNickname;
        [DataMember] private string mail;
        [DataMember] private string onPremisesSecurityIdentifier;
        
        protected AadGroup() { }

        private AadGroup(Guid objectId, string displayName, string description, string mailNickname, string mail, string onPremisesSecurityIdentifier)
            : base(objectId, displayName)
        {
            this.description = description;
            this.mailNickname = mailNickname;
            this.mail = mail;
            this.onPremisesSecurityIdentifier = onPremisesSecurityIdentifier;
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string MailNickname
        {
            get { return mailNickname; }
            set { mailNickname = value; }
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
        public string OnPremisesSecurityIdentifier
        {
            get { return onPremisesSecurityIdentifier; }
        }

        /// <summary>
        /// Creates immutable <see cref="AadGroup"/> objects.
        /// </summary>
        public class Factory
        {
            /// <summary>
            /// Creates an <see cref="AadGroup"/> object.
            /// </summary>
            public AadGroup Create()
            {
                return new AadGroup(ObjectId, DisplayName, Description, MailNickname, Mail, OnPremisesSecurityIdentifier);
            }

            public Guid ObjectId { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string MailNickname { get; set; }
            public string Mail { get; set; }
            public string OnPremisesSecurityIdentifier { get; set; }
        }
    }
}
