using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Users
{
    [DataContract]
    public class MailConfirmationParameters
    {
        /// <summary>
        /// The unique code that proves ownership of the email address.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String ChallengeCode { get; set; }

        /// <summary>
        /// The email address to be confirmed.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String MailAddress { get; set; }
    }
}
