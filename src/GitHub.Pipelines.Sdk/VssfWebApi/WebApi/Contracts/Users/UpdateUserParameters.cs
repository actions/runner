using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Runtime.Serialization;
using System.Linq;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Users
{
    /// <summary>
    /// Used for updating a user's data.
    /// </summary>
    [DataContract]
    public class UpdateUserParameters
    {
        /// <summary>
        /// Creates a new instance of an UpdateUserParameters object.
        /// </summary>
        public UpdateUserParameters()
        {
            this.Properties = new PropertiesCollection();
        }

        public UpdateUserParameters(UpdateUserParameters copy)
        {
            Descriptor = copy.Descriptor;
            Properties = new PropertiesCollection(copy.Properties);
            LastModified = copy.LastModified;
            Revision = copy.Revision;
        }

        /// <summary>
        /// The user's unique identifier, and the primary means by which the user is referenced.
        /// </summary>
        [IgnoreDataMember]
        public SubjectDescriptor Descriptor { get; set; }

        /// <summary>
        /// The collection of properties to set.  See "User" for valid fields.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public PropertiesCollection Properties
        {
            get; private set;
        }

        /// <summary>
        /// The user's name, as displayed throughout the product.
        /// </summary>
        [IgnoreDataMember]
        public String DisplayName
        {
            set { this.Properties[nameof(DisplayName)] = value; }
            get { return this.Properties.GetValue<String>(nameof(DisplayName), defaultValue: null); }
        }

        /// <summary>
        /// The user's preferred email address.
        /// </summary>
        [IgnoreDataMember]
        public String Mail
        {
            set { this.Properties[nameof(Mail)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Mail), defaultValue: null); }
        }

        /// <summary>
        /// The user's preferred email address which has not yet been confirmed.
        /// </summary>
        [IgnoreDataMember]
        public String UnconfirmedMail
        {
            set { this.Properties[nameof(UnconfirmedMail)] = value; }
            get { return this.Properties.GetValue<String>(nameof(UnconfirmedMail), defaultValue: null); }
        }

        /// <summary>
        /// The user's country of residence or association.
        /// </summary>
        [IgnoreDataMember]
        public String Country
        {
            set { this.Properties[nameof(Country)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Country), defaultValue: null); }
        }

        /// <summary>
        /// The region in which the user resides or is associated.
        /// </summary>
        [IgnoreDataMember]
        public String Region
        {
            set { this.Properties[nameof(Region)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Region), defaultValue: null); }
        }

        /// <summary>
        /// A short blurb of "about me"-style text.
        /// </summary>
        [IgnoreDataMember]
        public String Bio
        {
            set { this.Properties[nameof(Bio)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Bio), defaultValue: null); }
        }

        /// <summary>
        /// A link to an external blog.
        /// </summary>
        [IgnoreDataMember]
        public String Blog
        {
            set { this.Properties[nameof(Blog)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Blog), defaultValue: null); }
        }

        /// <summary>
        /// The company at which the user is employed.
        /// </summary>
        [IgnoreDataMember]
        public String Company
        {
            set { this.Properties[nameof(Company)] = value; }
            get { return this.Properties.GetValue<String>(nameof(Company), defaultValue: null); }
        }

        /// <summary>
        /// The date/time at which the user data was last modified.
        /// </summary>
        [IgnoreDataMember]
        internal DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// The user data revision, for change tracking.
        /// </summary>
        [IgnoreDataMember]
        internal Int32 Revision { get; set; }

        internal UpdateUserParameters Clone()
        {
            UpdateUserParameters clone = new UpdateUserParameters();

            clone.Descriptor = this.Descriptor;
            clone.Properties = new PropertiesCollection(this.Properties);
            clone.Revision = this.Revision;

            return clone;
        }

        internal virtual User ToUser()
        {
            User user = new User
            {
                Descriptor = this.Descriptor,
                LastModified = this.LastModified,
                Revision = this.Revision,
            };

            user.UpdateWith(this);

            return user;
        }

        public override string ToString()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                @"UpdateUserParameters
[
Descriptor:     {0}
Revision:       {1}
LastModified:   {2}
{3}
]",
                this.Descriptor,
                this.Revision,
                this.LastModified,
                String.Join("\r\n", Properties.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
        }
    }
}
