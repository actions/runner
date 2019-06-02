using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Profile
{
    public static class ProfileExtensions
    {
        /// <summary>
        /// Updates the the profile by applying the changes in the parameter <paramref name="changedApplicationAttributes"/>.
        /// </summary>
        public static void ApplyAttributesChanges(this Profile profile, IList<ProfileAttribute> changedApplicationAttributes, IList<CoreProfileAttribute> changedCoreAttributes)
        {
            ValidateProfile(profile);
            if ((changedApplicationAttributes == null || changedApplicationAttributes.Count == 0) && ((changedCoreAttributes == null || changedCoreAttributes.Count == 0)))
            {
                return;
            }
            ValidateAttributesApplicability(profile, changedApplicationAttributes, changedCoreAttributes);

            if (profile.ApplicationContainer == null && changedApplicationAttributes!=null && changedApplicationAttributes.Count > 0)
            {
                var containerName = changedApplicationAttributes.First().Descriptor.ContainerName;
                profile.ApplicationContainer = new AttributesContainer(containerName);
            }
            UpdateCoreAttributes(profile, changedCoreAttributes);
            UpdateApplicationAttributes(profile, changedApplicationAttributes);
        }

		private static void UpdateCoreAttributes(Profile profile, IEnumerable<CoreProfileAttribute> changedAttributes)
        {
            if (changedAttributes == null || !changedAttributes.Any())
            {
                return;
            }

            var existingAttributes = profile.CoreAttributes;
            foreach (var changedAttribute in changedAttributes)
            {
                CoreProfileAttribute existingAttribute;
                if (existingAttributes.TryGetValue(changedAttribute.Descriptor.AttributeName, out existingAttribute))
                {
                    if (existingAttribute.Revision < changedAttribute.Revision)
                    {
                        // Null value implies that the attribute was deleted on the server.
                        if (changedAttribute.Value == null)
                        {
                            existingAttributes.Remove(changedAttribute.Descriptor.AttributeName);
                        }
                        else
                        {
                            existingAttribute.Value = changedAttribute.Value;
                            existingAttribute.Revision = changedAttribute.Revision;
                        }
                    }
                }
                else
                {
                    if (changedAttribute.Value != null)
                    {
                        existingAttributes.Add(changedAttribute.Descriptor.AttributeName, changedAttribute);
                    }
                }
                if (changedAttribute.Revision > profile.CoreRevision)
                {
                    profile.CoreRevision = changedAttribute.Revision;
                }
                if (changedAttribute.Revision > profile.Revision)
                {
                    profile.Revision = changedAttribute.Revision;
                }
            }
        }

		internal static void UpdateApplicationAttributes(Profile profile, IEnumerable<ProfileAttribute> changedAttributes)
        {
            if (changedAttributes == null || !changedAttributes.Any())
            {
                return;
            }
            var existingAttributes = profile.ApplicationContainer.Attributes;
            foreach (var changedAttribute in changedAttributes)
            {
                ProfileAttribute existingAttribute;
                if (existingAttributes.TryGetValue(changedAttribute.Descriptor.AttributeName, out existingAttribute))
                {
                    if (existingAttribute.Revision < changedAttribute.Revision)
                    {
                        // Null value implies that the attribute was deleted on the server.
                        if (changedAttribute.Value == null)
                        {
                            existingAttributes.Remove(changedAttribute.Descriptor.AttributeName);
                        }
                        else
                        {
                            existingAttribute.Value = changedAttribute.Value;
                            existingAttribute.Revision = changedAttribute.Revision;
                        }
                    }
                }
                else
                {
                    if (changedAttribute.Value != null)
                    {
                        existingAttributes.Add(changedAttribute.Descriptor.AttributeName, changedAttribute);
                    }
                }
                if (changedAttribute.Revision > profile.ApplicationContainer.Revision)
                {
                    profile.ApplicationContainer.Revision = changedAttribute.Revision;
                }
                if (changedAttribute.Revision > profile.Revision)
                {
                    profile.Revision = changedAttribute.Revision;
                }
            }
        }

		private static void ValidateAttributesApplicability(Profile profile, IEnumerable<ProfileAttribute> changedAttributes, IEnumerable<CoreProfileAttribute> changedCoreAttributes)
        {
            if (changedCoreAttributes != null)
            {
                ProfileUtility.ValidateAttributesMetadata(changedCoreAttributes);
                ProfileUtility.ValidateAttributesBelongToCoreContainer(changedCoreAttributes);
            }
            if (changedAttributes != null)
            {
                ProfileUtility.ValidateAttributesMetadata(changedAttributes);
                var applicationContainerName = (profile.ApplicationContainer != null) ? profile.ApplicationContainer.ContainerName : null;
                ProfileUtility.ValidateAttributesBelongToOneApplicaitonContainer(changedAttributes, applicationContainerName);
            }
        }

		private static void ValidateProfile(Profile profile)
        {
            ArgumentUtility.CheckForNull(profile, "profile");

            if (profile.ApplicationContainer == null)
            {
                return;
            }
            foreach (var kv in profile.CoreAttributes.Where(kv => kv.Value.Revision > profile.Revision))
            {
                throw new ArgumentException(
                    String.Format("Current profile object is in a bad state. The revision of attribute '{0}': '{1}' is greater than the revision of the profile object: {2}",
                                  kv.Key, kv.Value.Revision, profile.Revision));
            }
            foreach (var kv in profile.ApplicationContainer.Attributes.Where(kv => kv.Value.Revision > profile.Revision))
            {
                throw new ArgumentException(
                    String.Format("Current profile object is in a bad state. The revision of attribute '{0}': '{1}' is greater than the revision of the profile object: {2}",
                                  kv.Key, kv.Value.Revision, profile.Revision));
            }
        }
    }
}
