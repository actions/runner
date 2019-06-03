using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    /// <summary>
    /// A user profile.
    /// </summary>
    [DataContract]
    public class Profile : ITimeStamped, IVersioned, ICloneable
    {
        public Profile()
        {
            CoreAttributes = new Dictionary<string, CoreProfileAttribute>(VssStringComparer.AttributesDescriptor);
        }

        public string DisplayName
        {
            get { return GetAttributeFromCoreContainer<string>(CoreAttributeNames.DisplayName, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.DisplayName, value); }
        }

        public string PublicAlias
        {
            get { return GetAttributeFromCoreContainer<string>(CoreAttributeNames.PublicAlias, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.PublicAlias, value); }
        }

        public string CountryName
        {
            get { return GetAttributeFromCoreContainer<string>(CoreAttributeNames.CountryName, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.CountryName, value); }
        }

        public string EmailAddress
        {
            get { return GetAttributeFromCoreContainer<string>(CoreAttributeNames.EmailAddress, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.EmailAddress, value); }
        }

        public string UnconfirmedEmailAddress
        {
            get { return GetAttributeFromCoreContainer<string>(CoreAttributeNames.UnconfirmedEmailAddress, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.UnconfirmedEmailAddress, value); }
        }

        public DateTimeOffset CreatedDateTime
        {
            get { return GetAttributeFromCoreContainer<DateTimeOffset>(CoreAttributeNames.DateCreated, default(DateTimeOffset)); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.DateCreated, value); }
        }

        public Avatar Avatar
        {
            get { return GetAttributeFromCoreContainer<Avatar>(CoreAttributeNames.Avatar, null); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.Avatar, value); }
        }

        /// <summary>
        /// The attributes of this profile.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public AttributesContainer ApplicationContainer { get; set; }

        /// <summary>
        /// The core attributes of this profile.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        internal IDictionary<string, CoreProfileAttribute> CoreAttributes { get; set; }

        /// <summary>
        /// The maximum revision number of any attribute.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public int CoreRevision { get; set; }

        /// <summary>
        /// The time at which this profile was last changed.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// The unique identifier of the profile.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid Id { get; internal set; }

        /// <summary>
        /// The maximum revision number of any attribute.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Revision { get; set; }

        /// <summary>
        /// The current state of the profile.
        /// </summary>
        [DataMember(IsRequired = false)]
        public ProfileState ProfileState { get; set; }

        public int TermsOfServiceVersion
        {
            get { return GetAttributeFromCoreContainer(CoreAttributeNames.TermsOfServiceVersion, 0); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.TermsOfServiceVersion, value); }
        }

        public DateTimeOffset TermsOfServiceAcceptDate
        {
            get { return GetAttributeFromCoreContainer<DateTimeOffset>(CoreAttributeNames.TermsOfServiceAcceptDate, default(DateTimeOffset)); }
            set { SetAttributeInCoreContainer(CoreAttributeNames.TermsOfServiceAcceptDate, value); }
        }

        public bool? ContactWithOffers
        {
            get
            {
                CoreProfileAttribute attribute;
                CoreAttributes.TryGetValue(CoreAttributeNames.ContactWithOffers, out attribute);
                if (attribute != null && attribute.Value != null && attribute.Value is bool)
                {
                    return (bool?)attribute.Value;
                }
                return null;
            }
            set { SetAttributeInCoreContainer(CoreAttributeNames.ContactWithOffers, value); }
        }

        private T GetAttributeFromCoreContainer<T>(string attributeName, T defaultValue)
        {
            CoreProfileAttribute attribute;
            CoreAttributes.TryGetValue(attributeName, out attribute);

            if (attribute != null && attribute.Value != null && attribute.Value.GetType() == typeof(T))
            {
                return (T)attribute.Value;
            }
            return defaultValue;
        }

        private void SetAttributeInCoreContainer(string attributeName, object value)
        {
            CoreProfileAttribute attribute;
            if (CoreAttributes.TryGetValue(attributeName, out attribute))
            {
                attribute.Value = value;
            }
            else
            {
                CoreAttributes.Add(attributeName, new CoreProfileAttribute()
                {
                    Descriptor = new AttributeDescriptor(CoreContainerName, attributeName),
                    Value = value,
                });
            }
        }

        public CoreProfileAttribute GetCoreAttribute(string attributeName)
        {
            CoreProfileAttribute attribute;
            CoreAttributes.TryGetValue(attributeName, out attribute);
            if (attribute == null)
            {
                return null;
            }
            return (CoreProfileAttribute)attribute.Clone();
        }

        public object Clone()
        {
            Profile newProfile = MemberwiseClone() as Profile;

            // Since core attributes are cloned on read, we can get away with a shallow copy
            newProfile.CoreAttributes = CoreAttributes != null ? CoreAttributes.ToDictionary(x => x.Key, x => (CoreProfileAttribute) x.Value.Clone()) : null;
            newProfile.ApplicationContainer = ApplicationContainer != null ? (AttributesContainer)ApplicationContainer.Clone() : null;

            return newProfile;
        }

        internal const string CoreContainerName = "Core";

        internal class CoreAttributeNames
        {
            internal const string DisplayName = "DisplayName";
            internal const string PublicAlias = "PublicAlias";
            internal const string EmailAddress = "EmailAddress";
            internal const string DefaultEmailAddress = "DefaultEmailAddress";
            internal const string UnconfirmedEmailAddress = "UnconfirmedEmailAddress";
            internal const string CountryName = "CountryName";
            internal const string Avatar = "Avatar";
            internal const string TermsOfServiceVersion = "TermsOfServiceVersion";
            internal const string TermsOfServiceAcceptDate = "TermsOfServiceAcceptDate";
            internal const string ContactWithOffers = "ContactWithOffers";
            internal const string DateCreated = "DateCreated";

            internal static readonly List<string> AttributeNameList = new List<string>()
                {
                    DisplayName,
                    PublicAlias,
                    EmailAddress,
                    UnconfirmedEmailAddress,
                    CountryName,
                    Avatar,
                    TermsOfServiceVersion,
                    TermsOfServiceAcceptDate,
                    ContactWithOffers,
                    DateCreated
                };
        }
    }

    /// <summary>
    /// The state of a profile.
    /// </summary>
    public enum ProfileState
    {
        /// <summary>
        /// The profile is in use.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// The profile is in use, but can only be read.
        /// </summary>
        CustomReadOnly = 1,

        /// <summary>
        /// The profile may only be read.
        /// </summary>
        ReadOnly = 2
    }
}
