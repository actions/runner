using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Users
{
    [DataContract]
    public class User
    {
        public User()
        {            
        }

        public User(User copy)
        {
            Descriptor = copy.Descriptor;
            UserName = copy.UserName;
            DisplayName = copy.DisplayName;
            Mail = copy.Mail;
            UnconfirmedMail = copy.UnconfirmedMail;
            Bio = copy.Bio;
            Blog = copy.Blog;
            Company = copy.Company;
            Country = copy.Country;
            DateCreated = copy.DateCreated;
            Links = copy.Links;
            LastModified = copy.LastModified;
            Revision = copy.Revision;
            State = copy.State;
        }

        /// <summary>
        /// The user's unique identifier, and the primary means by which the user is referenced.
        /// </summary>
        [DataMember(IsRequired = true)]
        public SubjectDescriptor Descriptor { get; set; }

        /// <summary>
        /// The unique name of the user.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String UserName { get; set; }

        /// <summary>
        /// The user's name, as displayed throughout the product.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String DisplayName { get; set; }

        /// <summary>
        /// The user's preferred email address.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String Mail { get; set; }

        /// <summary>
        /// The user's preferred email address which has not yet been confirmed.
        /// Do not use this as an email destination, instead prefer the
        /// confirmed email address <see cref="Mail"/>
        /// </summary>
        [DataMember(IsRequired = false)]
        public String UnconfirmedMail { get; set; }

        /// <summary>
        /// A short blurb of "about me"-style text.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Bio { get; set; }

        /// <summary>
        /// A link to an external blog.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Blog { get; set; }

        /// <summary>
        /// The company at which the user is employed.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Company { get; set; }

        /// <summary>
        /// The user's country of residence or association.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String Country { get; set; }

        /// <summary>
        /// The date the user was created in the system
        /// </summary>
        [DataMember(IsRequired = false)]
        public DateTimeOffset DateCreated { get; set; }

        /// <summary>
        /// A set of readonly links for obtaining more info about the user.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ReferenceLinks Links { get; internal set; }

        /// <summary>
        /// The date/time at which the user data was last modified.
        /// </summary>
        [DataMember(IsRequired = false)]
        public DateTimeOffset LastModified { get; internal set; }

        /// <summary>
        /// The attribute's revision, for change tracking.
        /// </summary>
        [DataMember(IsRequired = false)]
        public Int32 Revision { get; internal set; }

        /// <summary>
        /// The status of the user
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public UserState State { get; internal set; }

        /// <summary>
        /// Enumeration for the user status
        /// </summary>
        [DataContract]
        public enum UserState
        {
            Wellformed=0,
            PendingProfileCreation,
            Deleted,
        }

        public static implicit operator UpdateUserParameters(User user)
        {
            return new UpdateUserParameters
            {
                Descriptor = user.Descriptor,
                DisplayName = user.DisplayName,
                Mail = user.Mail,
                UnconfirmedMail = user.UnconfirmedMail,
                Country = user.Country,
                Bio = user.Bio,
                Blog = user.Blog,
                Company = user.Company,
                LastModified = user.LastModified,
                Revision = user.Revision,
            };
        }

        internal virtual void UpdateWith(UpdateUserParameters userParameters)
        {
            ArgumentUtility.CheckForNull(userParameters, nameof(userParameters));

            foreach (String propertyName in userParameters.Properties.Keys)
            {
                String value = userParameters.Properties[propertyName] as String;
                switch (propertyName)
                {
                    case (nameof(DisplayName)):
                        DisplayName = value;
                        break;

                    case (nameof(Mail)):
                        Mail = value;
                        break;

                    case (nameof(UnconfirmedMail)):
                        UnconfirmedMail = value;
                        break;

                    case (nameof(Country)):
                        Country = value;
                        break;

                    case (nameof(Bio)):
                        Bio = value;
                        break;

                    case (nameof(Blog)):
                        Blog = value;
                        break;

                    case (nameof(Company)):
                        Company = value;
                        break;
                }
            }
        }
    }
}
