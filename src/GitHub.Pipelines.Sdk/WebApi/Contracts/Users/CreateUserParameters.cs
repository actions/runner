using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Users
{
    /// <summary>
    /// Used at the time of initial user creation.
    /// </summary>
    [DataContract]
    public class CreateUserParameters
    {
        public CreateUserParameters()
        {
        }

        public CreateUserParameters(CreateUserParameters copy)
        {
            Descriptor = copy.Descriptor;
            DisplayName = copy.DisplayName;
            Mail = copy.Mail;
            Country = copy.Country;
            Region = copy.Region;
            PendingProfileCreation = copy.PendingProfileCreation; 

            if (copy.Data != null)
            {
                Data = new Dictionary<String, Object>(copy.Data);
            }
        }

        /// <summary>
        /// The user's unique identifier, and the primary means by which the user is referenced.
        /// </summary>
        [DataMember(IsRequired = true)]
        public SubjectDescriptor Descriptor { get; set; }

        /// <summary>
        /// The user's name, as displayed throughout the product.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String DisplayName { get; set; }

        /// <summary>
        /// The user's preferred email address.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Mail { get; set; }

        /// <summary>
        /// The user's country of residence or association.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Country { get; set; }

        /// <summary>
        /// The region in which the user resides or is associated.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Region { get; set; }

        /// <summary>
        /// Identifier to mark whether user's profile is pending
        /// </summary>
        [DataMember(IsRequired = false)]
        public Boolean PendingProfileCreation { get; set; }

        #region Legacy Data

        [DataMember(IsRequired = false)]
        public Dictionary<String, Object> Data { get; set; }

#endregion

        internal CreateUserParameters Clone()
        {
            return new CreateUserParameters(this);
        }

        internal virtual User ToUser()
        {
            return new User
            {
                Descriptor = this.Descriptor,
                DisplayName = this.DisplayName,
                Mail = this.Mail,
                Country = this.Country
            };
        }
    }
}
