using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.Profile
{
    internal static class ProfileUtility
    {
        internal static object GetCorrectlyTypedCoreAttribute(string coreAttributeName, object value)
        {
            if (VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.ContactWithOffers, coreAttributeName))
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            if (VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.TermsOfServiceVersion, coreAttributeName))
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            if (VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.TermsOfServiceAcceptDate, coreAttributeName) 
                || VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.DateCreated, coreAttributeName))
            {
                if (value is DateTimeOffset)
                {
                    return value;
                } 
                if (value is DateTime)
                {
                    return new DateTimeOffset((DateTime) value);
                }

                DateTimeOffset parsedValue;
                return DateTimeOffset.TryParse(value as string, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsedValue)
                            ? parsedValue
                            : default(DateTimeOffset);
            }
            if (VssStringComparer.AttributesDescriptor.Compare(Profile.CoreAttributeNames.Avatar, coreAttributeName) == 0)
            {
                if (value is Avatar)
                {
                    return value;
                }
                if (value is string)
                {
                    return JObject.Parse((string) value).ToObject<Avatar>();
                } 
                return JObject.FromObject(value).ToObject<Avatar>();
            }

            if (VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.DisplayName, coreAttributeName)
                || VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.CountryName, coreAttributeName)
                || VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.EmailAddress, coreAttributeName)
                || VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.UnconfirmedEmailAddress, coreAttributeName)
                || VssStringComparer.AttributesDescriptor.Equals(Profile.CoreAttributeNames.PublicAlias, coreAttributeName))
            {
                return value;
            }

            throw new ArgumentException(string.Format(_coreAttributeNotSupported, coreAttributeName));
        }


        internal static CoreProfileAttribute ExtractCoreAttribute<T>(ProfileAttributeBase<T> attribute)
        {
            return new CoreProfileAttribute
            {
                Descriptor = attribute.Descriptor,
                Revision = attribute.Revision,
                TimeStamp = attribute.TimeStamp,
                Value = GetCorrectlyTypedCoreAttribute(attribute.Descriptor.AttributeName, attribute.Value),
            };
        }

        internal static ProfileAttribute ExtractApplicationAttribute(ProfileAttributeBase<object> attribute)
        {
            return new ProfileAttribute
            {
                Descriptor = attribute.Descriptor,
                Revision = attribute.Revision,
                TimeStamp = attribute.TimeStamp,
                Value = attribute.Value as string,
            };
        }

        internal static void ValidateAttributes<T>(IEnumerable<ProfileAttributeBase<T>> attributes, string applicationContainerName = null)
        {
            if (attributes == null)
            {
                return;
            }
            ValidateAttributesMetadata(attributes);
            ValidateAttributesAreEitherCoreAttributesOrBelongToOneApplicationContainer(attributes, applicationContainerName);
        }

        internal static void ValidateAttributesMetadata<T>(IEnumerable<ProfileAttributeBase<T>> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            foreach (var attribute in attributes)
            {
                if (attribute.Descriptor == null || attribute.Descriptor.AttributeName == null || attribute.Descriptor.ContainerName == null ||
                    attribute.Revision < 0)
                {
                    if (attribute.Descriptor == null)
                    {
                        throw new ArgumentException(_attributeMissingNecessaryInformationErrorMessage, "attributes");
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(_attributeMissingNecessaryInformationDetailedErrorMessage, attribute.Descriptor), "attributes");
                    }
                }
            }
        }

        internal static void ValidateAttributesBelongToCoreContainer(IEnumerable<CoreProfileAttribute> coreAttributes)
        {
			foreach (var changedAttribute in coreAttributes.Where(changedAttribute => VssStringComparer.AttributesDescriptor.Compare(changedAttribute.Descriptor.ContainerName, Profile.CoreContainerName) != 0))
            {
				throw new ArgumentException(string.Format(_attributeDoesNotBelongToContainerErrorMessage, changedAttribute.Descriptor, Profile.CoreContainerName), "coreAttributes");
            }
        }

        private static void ValidateAttributesAreEitherCoreAttributesOrBelongToOneApplicationContainer<T>(IEnumerable<ProfileAttributeBase<T>> attributes, string applicationContainerName = null)
        {
            foreach (var attribute in attributes.Where(changedAttribute => VssStringComparer.AttributesDescriptor.Compare(changedAttribute.Descriptor.ContainerName, Profile.CoreContainerName) != 0))
            {
                if (applicationContainerName == null)
                {
                    applicationContainerName = attribute.Descriptor.ContainerName;
                }
                else if (VssStringComparer.AttributesDescriptor.Compare(applicationContainerName, attribute.Descriptor.ContainerName) != 0)
                {
                    throw new ArgumentException(string.Format(_attributesBelongingToDifferentContainersErrorMessage, applicationContainerName, attribute.Descriptor.ContainerName), "attributes");
                }
            }
        }

        internal static void ValidateAttributesBelongToOneApplicaitonContainer(IEnumerable<ProfileAttribute> attributes, string applicationContainerName = null)
        {
			if (VssStringComparer.AttributesDescriptor.Compare(applicationContainerName, Profile.CoreContainerName) == 0)
            {
				throw new ArgumentException(string.Format(_applicationContainerNameCannotBeErrorMessage, Profile.CoreContainerName), "applicationContainerName");
            }
            foreach (var attribute in attributes)
            {
				if (VssStringComparer.AttributesDescriptor.Compare(attribute.Descriptor.ContainerName, Profile.CoreContainerName) == 0)
                {
					throw new ArgumentException(string.Format(_anAplicationAttributesBelongsToContainerErrorMessage, attribute.Descriptor, Profile.CoreContainerName), "attributes");
                }
                if (applicationContainerName == null)
                {
                    applicationContainerName = attribute.Descriptor.ContainerName;
                }
                else if (VssStringComparer.AttributesDescriptor.Compare(applicationContainerName, attribute.Descriptor.ContainerName) != 0)
                {
                    throw new ArgumentException(string.Format(_attributesBelongingToDifferentContainersErrorMessage, applicationContainerName, attribute.Descriptor.ContainerName), "attributes");
                }
            }
        }
        private const string _attributesBelongingToDifferentContainersErrorMessage= "Collection of attributes has one attribute belonging to container '{0}' and another belonging to container '{1}'.";
        private const string _attributeDoesNotBelongToContainerErrorMessage = "Attribute '{0}' does not belong to container '{1}'.";
        private const string _applicationContainerNameCannotBeErrorMessage = "Application container name cannot be '{0}'.";
        private const string _anAplicationAttributesBelongsToContainerErrorMessage = "One of the application attributes '{0}' belongs to container '{1}.'";
        private const string _attributeMissingNecessaryInformationErrorMessage = "One of the attributes is missing necessary information to perform this operation.";
        private const string _attributeMissingNecessaryInformationDetailedErrorMessage = "The attribute '{0}' is missing necessary information to perform this operation.";
        private const string _coreAttributeNotSupported = "Core attribute '{0}' is not supported.";
    }
}