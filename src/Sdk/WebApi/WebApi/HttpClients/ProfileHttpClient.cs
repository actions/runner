using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Profile.Client
{
    [ClientCircuitBreakerSettings(timeoutSeconds: 100, failurePercentage: 80, MaxConcurrentRequests = 40)]
    public class ProfileHttpClient : ProfileHttpClientBase
    {
        public const int MaxAttributeValueLength = 1024 * 1024;

        public ProfileHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ProfileHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ProfileHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ProfileHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ProfileHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// Updates (conditionally) the value of a profile attribute if it already exists.
        /// </summary>
        /// <remarks>If attribute does not exist then a new attribute is added.
        /// If the timestamp in the parameter <paramref name="newAttribute"/> is not the default value of <see cref="DateTimeOffset"/>, 
        /// then the attribute is only updated if the paramter <paramref name="newAttribute"/> has a newer timestamp than the attribute stored in the server.
        /// </remarks> 
        /// <exception cref="NewerVersionOfResourceExistsException">If the attribute in the server has a newer timestamp.</exception>
        public virtual Task<int> SetAttributeAsync(ProfileAttribute newAttribute, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SetAttributeAsync(newAttribute, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns an attribute with descriptor = <paramref name="descriptor"/>.
        /// </summary>
        /// <param name="descriptor">Descriptor of the attribute</param>
        public virtual Task<ProfileAttribute> GetAttributeAsync(AttributeDescriptor descriptor, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAttributeAsync(descriptor, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes (conditionally) a profile attribute.
        /// </summary>
        /// <remarks>
        /// If the timestamp in the parameter <paramref name="attributeToDelete"/> is not the default value of <see cref="DateTimeOffset"/>, 
        /// then the attribute is only deleted if the paramter <paramref name="attributeToDelete"/> has a timestamp which is not older than the
        /// timestamp of the attribute in the server.
        /// </remarks>
        /// <exception cref="ProfileAttributeNotFoundException">If named attribute does not exist on the server.</exception>
        public virtual Task<int> DeleteAttributeAsync(ProfileAttribute attributeToDelete, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.DeleteAttributeAsync(attributeToDelete, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns the profile of the authenticated identity.
        /// </summary>
        public virtual Task<Profile> GetProfileAsync(ProfileQueryContext profileQueryContext, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetProfileAsync(profileQueryContext, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Partially updates the content of a profile by comparing changes with the contents of a given profile in parameter <paramref name="profile"/>.
        /// </summary>
        /// <remarks>
        /// A profile property is not updated if the property is set to null in parameter <paramref name="profile"/>.
        /// A profile attribute is not updated if the attribute is missing from the list of attributes in <paramref name="profile"/>.
        /// </remarks>
        /// <exception cref="NewerVersionOfProfileExists">If the revision in the parameter <paramref name="profile"/> does not match to the current revision</exception>
        /// <param name="id">The Guid of the Identity with which the Profile is associated. There exists a 1 to 1 mapping between an Identity and a Profile</param>
        /// <param name="profile">Container object that contains the changes to be applied to the profile</param>
        /// <returns>The revision of the updated profile</returns>
		public virtual Task<int> UpdateProfileAsync(Profile profile, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.UpdateProfileAsync(profile, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns the avatar in the profile of the authenticated identity.
        /// </summary>
        /// <param name="size">Parameter to specify the desired size for the avatar.</param>
        public virtual Task<Avatar> GetAvatarAsync(AvatarSize size = AvatarSize.Medium, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAvatarAsync(size, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns the avatar in the profile of the authenticated identity given in parameter <paramref name="id"/>.
        /// </summary>
        /// <param name="size">Parameter to specify the desired size for the avatar.</param>
        /// <param name="id"> Parameter to specify the ID corresponding to the profile avatar requested</param>
        public virtual Task<Avatar> GetAvatarAsync(Guid id, AvatarSize size = AvatarSize.Medium, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAvatarAsync(size, id.ToString(), userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns the avatar in the profile of the authenticated identity.
        /// </summary>
        /// <param name="currentCopy">Parameter to specify the current copy of the avatar. 
        /// If the server does not have a newer version of the avatar then avatar objects are not sent over the wire.</param>
        public virtual Task<Avatar> GetAvatarAsync(Avatar currentCopy, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAvatarAsync(currentCopy, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Updates the avatar conditionally.
        /// </summary>
        /// <remarks>
        /// If the timestamp in the parameter <paramref name="newAvatar"/> is not the default value of <see cref="DateTimeOffset"/>, 
        /// then the avatar is only updated if the paramter <paramref name="newAvatar"/> has a newer timestamp than the avatar stored in the server.
        /// </remarks>
        /// <exception cref="NewerVersionOfResourceExistsException">If the avatar in the server has a newer timestamp.</exception>
        public virtual Task<int> SetAvatarAsync(Avatar newAvatar, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SetAvatarAsync(newAvatar, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns the display name of the profile of the authenticated identity.
        /// </summary>
        public virtual Task<string> GetDisplayNameAsync(object userState = null)
        {
            return GetDisplayNameImplAsync(userState, default(CancellationToken));
        }

        /// <summary>
        /// Returns the display name of the profile of the authenticated identity.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<string> GetDisplayNameAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDisplayNameImplAsync(userState, cancellationToken);
        }

        private async Task<string> GetDisplayNameImplAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Guid locationId = ProfileResourceIds.DisplayNameLocationid;

            var response = await SendAsync(
                method: HttpMethod.Get,
                locationId: locationId,
                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = ProfileRestApiConstants.Me },
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the attributes stored under the profile of the authenticated identity.
        /// </summary>
        /// <remarks>
        /// If an attribute has been deleted since the point in time specified in <paramref name="attributesQueryContext"/>, then the attribute is returned
        /// with it's value set to null.
        /// </remarks>
        public virtual Task<Tuple<IList<ProfileAttribute>, IList<CoreProfileAttribute>>> GetAttributesAsync(AttributesQueryContext attributesQueryContext, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAttributesAsync(attributesQueryContext, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        public virtual Task<int> SetAttributesAsync(IList<ProfileAttribute> applicationAttributes, IList<CoreProfileAttribute> coreAttributes, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SetAttributesAsync(applicationAttributes, coreAttributes, ProfileRestApiConstants.Me, userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns requested service setting
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new virtual Task<string> GetServiceSettingAsync(string settingName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetServiceSettingAsync(settingName, userState, cancellationToken);
        }        

        /// <summary>
        /// Returns the location to a profile page.
        /// </summary>
        /// <param name="profilePageType"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new virtual Task<string> GetProfileLocationsAsync(ProfilePageType profilePageType, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetProfileLocationsAsync(profilePageType, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Create profile
        /// </summary>
        /// <param name="createProfileContext">Context for profile creation</param>
        /// <param name="autoCreate">Create profile automatically</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public new virtual Task<Profile> CreateProfileAsync(
            CreateProfileContext createProfileContext,
            bool? autoCreate = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.CreateProfileAsync(createProfileContext, autoCreate, userState, cancellationToken);
        }
    }
}
