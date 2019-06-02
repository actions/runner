using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Profile.Client
{
    [ResourceArea(ProfileResourceIds.AreaId)]
    public abstract class ProfileHttpClientBase : VssHttpClientBase
    {

        protected ProfileHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ProfileHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ProfileHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ProfileHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ProfileHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        protected virtual async Task<ProfileAttribute> GetAttributeAsync(AttributeDescriptor descriptor, string id = ProfileRestApiConstants.Me, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ValidateAttributeDescriptor(descriptor);

            try
            {
                return await SendAsync<ProfileAttribute>(
                    method: HttpMethod.Get,
                    locationId: ProfileResourceIds.AttributeLocationid,
                    routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id, descriptor = descriptor.ToString() },
                    version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (ProfileAttributeNotFoundException)
            {
                return null;
            }
        }

        protected virtual async Task<int> SetAttributeAsync(ProfileAttribute attribute,
                                                                        string id = ProfileRestApiConstants.Me,
                                                                        object userState = null,
                                                                        CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ValidateAttribute(attribute);

            var contentBody = new { value = attribute.Value };
            var content = new ObjectContent(contentBody.GetType(), contentBody, Formatter);

            var message = await CreateRequestMessageAsync(
                                method: HttpMethod.Put,
                                locationId: ProfileResourceIds.AttributeLocationid,
                                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id, descriptor = attribute.Descriptor.ToString() },
                                content: content,
                                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                                userState: userState,
                                cancellationToken: cancellationToken)
                                .ConfigureAwait(false);


            SetIfUnmodifiedSinceHeaders(attribute, message);
            SetIfMatchHeaders(attribute, message);

            var response = await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ExtractRevisionFromEtagInResponseHeader(response);
        }

        protected async virtual Task<int> DeleteAttributeAsync(ProfileAttribute attribute,
                                                                string id = ProfileRestApiConstants.Me,
                                                                object userState = null,
                                                                CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ValidateAttribute(attribute);

            var message = await CreateRequestMessageAsync(
                                method: HttpMethod.Delete,
                                locationId: ProfileResourceIds.AttributeLocationid,
                                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id, descriptor = attribute.Descriptor.ToString() },
                                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                                userState: userState,
                                cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

            SetIfUnmodifiedSinceHeaders(attribute, message);
            SetIfMatchHeaders(attribute, message);

            var response = await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ExtractRevisionFromEtagInResponseHeader(response);
        }

        protected async virtual Task<Profile> GetProfileAsync(ProfileQueryContext profileQueryContext,
                                                        string id = ProfileRestApiConstants.Me,
                                                        object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ArgumentUtility.CheckForNull(profileQueryContext, "attributesQueryContext");

            var queryParameters = new List<KeyValuePair<String, String>>();
            queryParameters.Add(QueryParameters.Details, "true");
            queryParameters.Add(QueryParameters.CoreAttributes, ConvertCoreAttributesFlagsToCommaDelimitedString(profileQueryContext.CoreAttributes));

            var profile = await SendAsync<Profile>(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.ProfileLocationid,
                routeValues: new { id = id },
                queryParameters: queryParameters,
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.ProfileResourceRtmVersion),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var kv in profile.CoreAttributes)
            {
                kv.Value.Value = ProfileUtility.GetCorrectlyTypedCoreAttribute(kv.Value.Descriptor.AttributeName, kv.Value.Value);
            }
            return profile;
        }

        protected async Task<int> UpdateProfileAsync(Profile profile,
                                                           string id = ProfileRestApiConstants.Me,
                                                           object userState = null,
                                                           CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ValidateProfile(profile);

            var content = new ObjectContent<Profile>(profile, base.Formatter);
            var response = await SendAsync(
                method: new HttpMethod("PATCH"),
                locationId: ProfileResourceIds.ProfileLocationid,
                routeValues: new { id = id },
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                content: content,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ExtractRevisionFromEtagInResponseHeader(response);
        }

        protected virtual Task<Avatar> GetAvatarAsync(AvatarSize size = AvatarSize.Medium,
                                                    string id = ProfileRestApiConstants.Me,
                                                    object userState = null,
                                                    CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            var queryParameters = new List<KeyValuePair<String, String>> { new KeyValuePair<String, String>(QueryParameters.Size, size.ToString()) };

            return SendAsync<Avatar>(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.AvatarLocationid,
                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                queryParameters: queryParameters,
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        protected virtual async Task<Avatar> GetAvatarAsync(Avatar avatar,
                                            string id = ProfileRestApiConstants.Me,
                                            object userState = null,
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ArgumentUtility.CheckForNull(avatar, "avatar");

            var queryParameters = new List<KeyValuePair<String, String>> { new KeyValuePair<String, String>(QueryParameters.Size, avatar.Size.ToString()) };
            var message = await CreateRequestMessageAsync(
                    method: HttpMethod.Get,
                    locationId: ProfileResourceIds.AvatarLocationid,
                    routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                    queryParameters: queryParameters,
                    version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                    userState: userState,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            SetIfModifiedSinceHeaders(avatar, message);
            try
            {
                return await SendAsync<Avatar>(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (VssServiceResponseException ex)
            {
                if (ex.HttpStatusCode == HttpStatusCode.NotModified)
                {
                    return avatar;
                }
                throw;
            }
        }

        protected async virtual Task<int> SetAvatarAsync(Avatar avatar,
                                                                string id = ProfileRestApiConstants.Me,
                                                                object userState = null,
                                                                CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            ArgumentUtility.CheckForNull(avatar, "avatar");

            var contentBody = new { value = avatar.Value };
            var content = new ObjectContent(contentBody.GetType(), contentBody, Formatter);
            var message = await CreateRequestMessageAsync(
                                method: HttpMethod.Put,
                                locationId: ProfileResourceIds.AvatarLocationid,
                                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                                content: content,
                                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                                userState: userState,
                                cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
            SetIfUnmodifiedSinceHeaders(avatar, message);

            var response = await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ExtractRevisionFromEtagInResponseHeader(response);
        }

        protected async virtual Task<int> ResetAvatarAsync(string id = ProfileRestApiConstants.Me,
                                                                object userState = null,
                                                                CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);

            var message = await CreateRequestMessageAsync(
                                method: HttpMethod.Delete,
                                locationId: ProfileResourceIds.AvatarLocationid,
                                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                                userState: userState,
                                cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

            var response = await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ExtractRevisionFromEtagInResponseHeader(response);
        }                     

        internal virtual async Task<IList<ProfileAttributeBase<object>>> GetAttributesInternalAsync(AttributesQueryContext attributesQueryContext, string id = ProfileRestApiConstants.Me,
                                                                                     object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);

            var queryParameters = new List<KeyValuePair<String, String>>();
            switch (attributesQueryContext.Scope)
            {
                case AttributesScope.Core:
                    queryParameters.Add(QueryParameters.Partition, Profile.CoreContainerName);
                    break;
                case AttributesScope.Application:
                    queryParameters.Add(QueryParameters.Partition, attributesQueryContext.ContainerName);
                    break;
                case AttributesScope.Core | AttributesScope.Application:
                    queryParameters.Add(QueryParameters.Partition, attributesQueryContext.ContainerName);
                    queryParameters.Add(QueryParameters.WithCoreAttributes, "true");
                    if (attributesQueryContext.CoreAttributes != null)
                    {
                        queryParameters.Add(QueryParameters.CoreAttributes, ConvertCoreAttributesFlagsToCommaDelimitedString(attributesQueryContext.CoreAttributes.Value));
                    }
                    break;
            }

            if (attributesQueryContext.ModifiedSince != null)
            {
                queryParameters.Add(QueryParameters.ModifiedSince, attributesQueryContext.ModifiedSince.Value.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " GMT");
            }

            if (attributesQueryContext.ModifiedAfterRevision != null)
            {
                queryParameters.Add(QueryParameters.ModifiedAfterRevision, attributesQueryContext.ModifiedAfterRevision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return await SendAsync<IList<ProfileAttributeBase<object>>>(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.AttributeLocationid,
                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                queryParameters: queryParameters,
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.AttributeResourceRcVersion),
                userState: userState,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

        protected virtual async Task<Tuple<IList<ProfileAttribute>, IList<CoreProfileAttribute>>> GetAttributesAsync(AttributesQueryContext attributesQueryContext, string id = ProfileRestApiConstants.Me, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var attributes = await GetAttributesInternalAsync(
                attributesQueryContext, id, userState, cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var applicationAttributes = new List<ProfileAttribute>();
            var coreAttributes = new List<CoreProfileAttribute>();
            var result = new Tuple<IList<ProfileAttribute>, IList<CoreProfileAttribute>>(applicationAttributes, coreAttributes);
            if (attributes == null || attributes.Count == 0)
            {
                return result;
            }
            try
            {
                ProfileUtility.ValidateAttributes(attributes);
            }
            catch (ArgumentException ex)
            {
                throw new ProfileException("The list of received attributes failed validation.", ex);
            }

            foreach (var attribute in attributes)
            {
                if (VssStringComparer.AttributesDescriptor.Compare(attribute.Descriptor.ContainerName, Profile.CoreContainerName) == 0)
                {
                    coreAttributes.Add(ProfileUtility.ExtractCoreAttribute(attribute));
                }
                else
                {
                    applicationAttributes.Add(ProfileUtility.ExtractApplicationAttribute(attribute));
                }
            }
            return result;
        }

        protected async Task<int> SetAttributesAsync(IList<ProfileAttribute> applicationAttributes, IList<CoreProfileAttribute> coreAttributes, string id = ProfileRestApiConstants.Me,
                                               object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateId(id);
            if ((applicationAttributes == null || applicationAttributes.Count == 0) && (coreAttributes == null || coreAttributes.Count == 0))
            {
                throw new ArgumentException(string.Format("Either param '{0}' or '{1}' should be non-empty", "applicationAttributes", "coreAttributes"));
            }

            var unifiedAttributeList = new List<object>();
            if (applicationAttributes != null)
            {
                foreach (var attribute in applicationAttributes)
                {
                    ValidateAttribute(attribute);
                    unifiedAttributeList.Add(attribute);
                }
            }
            if (coreAttributes != null)
            {
                foreach (var attribute in coreAttributes)
                {
                    ValidateAttribute(attribute);
                    unifiedAttributeList.Add(attribute);
                }
            }

            var content = new ObjectContent(unifiedAttributeList.GetType(), unifiedAttributeList, Formatter);

            var message = await CreateRequestMessageAsync(
                                method: new HttpMethod("PATCH"),
                                locationId: ProfileResourceIds.AttributeLocationid,
                                routeValues: new { parentresource = ProfileResourceIds.ProfileResource, id = id },
                                content: content,
                                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.AttributeResourcePreviewVersion),
                                userState: userState,
                                cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

            var response = await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);

            return ExtractRevisionFromEtagInResponseHeader(response);
        }        

        // Used by VS IDE Connected User to fetch Profile Web URL
        protected virtual Task<string> GetProfileLocationsAsync(
            ProfilePageType profilePageType,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryParameters = new List<KeyValuePair<String, String>> { new KeyValuePair<String, String>(QueryParameters.ProfilePageType, profilePageType.ToString()) };

            return SendAsync<string>(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.LocationsLocationid,
                queryParameters: queryParameters,
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        // Used by VS IDE to fetch ProfileRefreshInterval
        protected virtual async Task<string> GetServiceSettingAsync(string settingName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringLength(settingName, "settingName", 100);

            var response = await SendAsync(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.SettingsLocationid,
                routeValues: new { settingName = settingName },
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// [Preview API] Create profile
        /// </summary>
        /// <param name="createProfileContext">Context for profile creation</param>
        /// <param name="autoCreate">Create profile automatically</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<Profile> CreateProfileAsync(
            CreateProfileContext createProfileContext,
            bool? autoCreate = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!autoCreate.HasValue || !autoCreate.Value)
            {
                ValidateCreateProfileContext(createProfileContext);
            }

            HttpMethod httpMethod = HttpMethod.Post;
            Guid locationId = ProfileResourceIds.ProfileLocationid;
            HttpContent content = new ObjectContent<CreateProfileContext>(createProfileContext, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (autoCreate != null)
            {
                queryParams.Add("autoCreate", autoCreate.Value.ToString());
            }

            return SendAsync<Profile>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("3.0-preview.3"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }

        protected virtual Task<HttpResponseMessage> VerifyAndUpdatePreferredEmailAsync(VerifyPreferredEmailContext verifyPreferredEmailContext, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateVerifyPreferredEmailContext(verifyPreferredEmailContext);
            var content = new ObjectContent<VerifyPreferredEmailContext>(verifyPreferredEmailContext, base.Formatter);

            return SendAsync<HttpResponseMessage>(
                method: HttpMethod.Post,
                locationId: ProfileResourceIds.PreferredEmailConfirmationLocationid,
                version: new ApiResourceVersion(previewApiVersion, ProfileResourceVersions.GenericResourcePreviewVersion),
                content: content,
                userState: userState,
                cancellationToken: cancellationToken);
        }        

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<ProfileRegions> GetRegionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return SendAsync<ProfileRegions>(
                method: HttpMethod.Get,
                locationId: ProfileResourceIds.RegionsLocationId,
                version: new ApiResourceVersion("3.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        #region Helper methods

        private static void ValidateId(string id)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(id, "id");
            ArgumentUtility.CheckStringForInvalidCharacters(id, "id");
        }

        private static void ValidateAttributeDescriptor(AttributeDescriptor descriptor)
        {
            ArgumentUtility.CheckForNull(descriptor, "descriptor");
        }

        private static void ValidateAttribute<T>(ProfileAttributeBase<T> attribute)
        {
            ArgumentUtility.CheckForNull(attribute, "attribute");
            ValidateAttributeDescriptor(attribute.Descriptor);
        }

        private static void ValidateProfile(Profile profile)
        {
            ArgumentUtility.CheckForNull(profile, "profile");
        }

        private static void ValidateCreateProfileContext(CreateProfileContext createProfileContext)
        {
            ArgumentUtility.CheckForNull(createProfileContext, "createProfileContext");
            ArgumentUtility.CheckStringForNullOrEmpty(createProfileContext.DisplayName, "DisplayName", trim: true);
            ArgumentUtility.CheckStringForInvalidCharacters(createProfileContext.DisplayName, "DisplayName", allowCrLf: false);

            ArgumentUtility.CheckStringForNullOrEmpty(createProfileContext.CountryName, "CountryName", trim: true);
            ArgumentUtility.CheckStringForInvalidCharacters(createProfileContext.CountryName, "CountryName", allowCrLf: false);

            ArgumentUtility.CheckStringForNullOrEmpty(createProfileContext.EmailAddress, "EmailAddress", trim: true);
            ArgumentUtility.CheckStringForInvalidCharacters(createProfileContext.EmailAddress, "EmailAddress", allowCrLf: false);
        }

        private static void ValidateVerifyPreferredEmailContext(VerifyPreferredEmailContext verifyPreferredEmailContext)
        {
            ArgumentUtility.CheckForNull(verifyPreferredEmailContext, "verifyPreferredEmailContext");
            ArgumentUtility.CheckForEmptyGuid(verifyPreferredEmailContext.Id, "Id");
            ArgumentUtility.CheckStringForNullOrEmpty(verifyPreferredEmailContext.HashCode, "HashCode", trim: true);
            ArgumentUtility.CheckStringForNullOrEmpty(verifyPreferredEmailContext.HashCode, "HashCode", trim: true);
            ArgumentUtility.CheckStringForNullOrEmpty(verifyPreferredEmailContext.EmailAddress, "EmailAddress", trim: true);
            ArgumentUtility.CheckStringForInvalidCharacters(verifyPreferredEmailContext.EmailAddress, "EmailAddress", allowCrLf: false);
        }

        private static void SetIfModifiedSinceHeaders(ITimeStamped timeStampedResource, HttpRequestMessage message)
        {
            if (timeStampedResource != null && timeStampedResource.TimeStamp != default(DateTimeOffset))
            {
                message.Headers.IfModifiedSince = timeStampedResource.TimeStamp;
            }
        }

        private static void SetIfNoneMatchHeaders(string etagContent, HttpRequestMessage message)
        {
            if (!string.IsNullOrEmpty(etagContent))
            {
                var entityTagHeaderValue = EntityTagHeaderValue.Parse(string.Concat("\"", etagContent, "\""));
                message.Headers.IfNoneMatch.Add(entityTagHeaderValue);
            }
        }

        private static void SetIfMatchHeaders(IVersioned versionedResource, HttpRequestMessage message)
        {
            if (versionedResource == null || versionedResource.Revision == 0)
            {
                return;
            }
            var entityTagHeaderValue = EntityTagHeaderValue.Parse(string.Concat("\"", versionedResource.Revision.ToString(CultureInfo.InvariantCulture), "\""));
            message.Headers.IfMatch.Add(entityTagHeaderValue);
        }

        private static void SetIfUnmodifiedSinceHeaders(ITimeStamped timeStampedResource, HttpRequestMessage message)
        {
            if (timeStampedResource != null && timeStampedResource.TimeStamp != default(DateTimeOffset))
            {
                message.Headers.IfUnmodifiedSince = timeStampedResource.TimeStamp;
            }
        }

        private static int ExtractRevisionFromEtagInResponseHeader(HttpResponseMessage response)
        {
            try
            {
                var revision = response.Headers.ETag.Tag;
                revision = revision.Trim('"');
                return Convert.ToInt32(revision);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static string ConvertCoreAttributesFlagsToCommaDelimitedString(CoreProfileAttributes coreAttributes)
        {
            if (coreAttributes.HasFlag(CoreProfileAttributes.All))
            {
                Enum.GetName(typeof(CoreProfileAttributes), CoreProfileAttributes.All);
            }

            List<string> coreAttributeStrings = new List<string>();
            foreach (CoreProfileAttributes currentAttribute in Enum.GetValues(typeof(CoreProfileAttributes)))
            {
                if (coreAttributes.HasFlag(currentAttribute))
                {
                    coreAttributeStrings.Add(Enum.GetName(typeof(CoreProfileAttributes), currentAttribute));
                }
            }

            return string.Join(",", coreAttributeStrings);
        }

        #endregion
        #region State

        // Used for testing only
        internal int NotificationDelay { get; set; }

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        private static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>()
            {
            // 401 (Bad Request)
            {"BadProfileRequestException", typeof(BadProfileRequestException)},
                {"BadPublicAliasException", typeof(BadPublicAliasException)},
                {"BadDisplayNameException", typeof(BadDisplayNameException)},
                {"BadCountryNameException", typeof(BadCountryNameException)},
                {"BadEmailAddressException", typeof(BadEmailAddressException)},
                {"BadAvatarValueException", typeof(BadAvatarValueException)},
                {"BadAttributeValueException", typeof(BadAttributeValueException)},
                {"BadServiceSettingNameException", typeof(BadServiceSettingNameException)},

            // 403 (Forbidden)
            {"ProfileServiceSecurityException", typeof(ProfileServiceSecurityException)},
                {"ProfileNotAuthorizedException", typeof(ProfileNotAuthorizedException)},

            // 404 (NotFound)
            {"ProfileResourceNotFoundException", typeof(ProfileResourceNotFoundException)},
                {"ProfileAttributeNotFoundException", typeof(ProfileAttributeNotFoundException)},
                {"ProfileDoesNotExistException", typeof(ProfileDoesNotExistException)},
                {"ServiceSettingNotFoundException", typeof(ServiceSettingNotFoundException)},

            // 409 (Conflict)
            {"NewerVersionOfResourceExistsException", typeof(NewerVersionOfResourceExistsException)},
                {"NewerVersionOfProfileExists", typeof(NewerVersionOfProfileExists)},
                {"ProfileAlreadyExistsException", typeof(ProfileAlreadyExistsException)},
                {"PublicAliasAlreadyExistException", typeof(PublicAliasAlreadyExistException)},

            // 413 (RequestEntityTooLarge)
            {"AttributeValueTooBigException", typeof(AttributeValueTooBigException)},
                {"AvatarTooBigException", typeof(AvatarTooBigException)},

            // 501 (NotImplemented)
            {"ServiceOperationNotAvailableException", typeof(ServiceOperationNotAvailableException)},
            };

        protected static readonly Version previewApiVersion = new Version(5, 0);

        #endregion
    }
}
