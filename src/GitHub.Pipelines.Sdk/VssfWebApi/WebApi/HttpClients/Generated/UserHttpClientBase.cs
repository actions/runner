/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 *
 * ---------------------------------------------------------
 * Generated file, DO NOT EDIT
 * ---------------------------------------------------------
 *
 * See following wiki page for instructions on how to regenerate:
 *   https://aka.ms/azure-devops-client-generation
 *
 * Configuration file:
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\user.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Users.Client
{
    [ResourceArea(UserResourceIds.AreaId)]
    public abstract class UserHttpClientBase : UserCompatHttpClientBase
    {
        public UserHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Deletes an attribute for the given user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="attributeName">The name of the attribute to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task DeleteAttributeAsync(
            string descriptor,
            string attributeName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("ac77b682-1ef8-4277-afde-30af9b546004");
            object routeValues = new { descriptor = descriptor, attributeName = attributeName };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Retrieves an attribute for a given user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="attributeName">The name of the attribute to retrieve.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<UserAttribute> GetAttributeAsync(
            string descriptor,
            string attributeName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ac77b682-1ef8-4277-afde-30af9b546004");
            object routeValues = new { descriptor = descriptor, attributeName = attributeName };

            return SendAsync<UserAttribute>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieves attributes for a given user. May return subset of attributes providing continuation token to retrieve the next batch
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="continuationToken">The token telling server to return the next chunk of attributes from where it stopped last time. This must either be null or be a value returned from the previous call to this API</param>
        /// <param name="queryPattern">The wildcardable pattern for the attribute names to be retrieved, e.g. queryPattern=visualstudio.14.*</param>
        /// <param name="modifiedAfter">The optional date/time of the minimum modification date for attributes to be retrieved, e.g. modifiedafter=2017-04-12T15:00:00.000Z</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<UserAttributes> QueryAttributesAsync(
            string descriptor,
            string continuationToken = null,
            string queryPattern = null,
            DateTimeOffset? modifiedAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ac77b682-1ef8-4277-afde-30af9b546004");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (queryPattern != null)
            {
                queryParams.Add("queryPattern", queryPattern);
            }
            if (modifiedAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "modifiedAfter", modifiedAfter.Value);
            }

            return SendAsync<UserAttributes>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates multiple attributes for a given user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="attributeParametersList">The list of attribute data to update.  Existing values will be overwritten.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<UserAttribute>> SetAttributesAsync(
            string descriptor,
            IEnumerable<SetUserAttributeParameters> attributeParametersList,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("ac77b682-1ef8-4277-afde-30af9b546004");
            object routeValues = new { descriptor = descriptor };
            HttpContent content = new ObjectContent<IEnumerable<SetUserAttributeParameters>>(attributeParametersList, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<UserAttribute>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Deletes a user's avatar.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task DeleteAvatarAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("1c34cdf0-dd20-4370-a316-56ba776d75ce");
            object routeValues = new { descriptor = descriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Retrieves the user's avatar.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="size">The size to retrieve, e.g. small, medium (default), or large.</param>
        /// <param name="format">The format for the response. Can be null. Accepted values: "png", "json"</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<Avatar> GetAvatarAsync(
            string descriptor,
            AvatarSize? size = null,
            string format = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1c34cdf0-dd20-4370-a316-56ba776d75ce");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (size != null)
            {
                queryParams.Add("size", size.Value.ToString());
            }
            if (format != null)
            {
                queryParams.Add("format", format);
            }

            return SendAsync<Avatar>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Creates or updates an avatar to be associated with a given user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="avatar">The avatar to set. The Image property must contain the binary representation of the image, in either jpg or png format.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task SetAvatarAsync(
            string descriptor,
            Avatar avatar,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("1c34cdf0-dd20-4370-a316-56ba776d75ce");
            object routeValues = new { descriptor = descriptor };
            HttpContent content = new ObjectContent<Avatar>(avatar, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="avatar"></param>
        /// <param name="size"></param>
        /// <param name="displayName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<Avatar> CreateAvatarPreviewAsync(
            string descriptor,
            Avatar avatar,
            AvatarSize? size = null,
            string displayName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("aad154d3-750f-47e6-9898-dc3a2e7a1708");
            object routeValues = new { descriptor = descriptor };
            HttpContent content = new ObjectContent<Avatar>(avatar, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (size != null)
            {
                queryParams.Add("size", size.Value.ToString());
            }
            if (displayName != null)
            {
                queryParams.Add("displayName", displayName);
            }

            return SendAsync<Avatar>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<SubjectDescriptor> GetDescriptorAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e338ed36-f702-44d3-8d18-9cba811d013a");
            object routeValues = new { descriptor = descriptor };

            return SendAsync<SubjectDescriptor>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Confirms preferred email for a given user.
        /// </summary>
        /// <param name="descriptor">The descriptor identifying the user for the operation.</param>
        /// <param name="confirmationParameters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task ConfirmMailAsync(
            string descriptor,
            MailConfirmationParameters confirmationParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("fc213dcd-3a4e-4951-a2e2-7e3fed15706d");
            object routeValues = new { descriptor = descriptor };
            HttpContent content = new ObjectContent<MailConfirmationParameters>(confirmationParameters, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Returns a user's most recently accessed hosts.
        /// </summary>
        /// <param name="descriptor">A user descriptor</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<AccessedHost>> GetMostRecentlyAccessedHostsAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a72c0174-9db6-428d-8674-3e57ef050f3d");
            object routeValues = new { descriptor = descriptor };

            return SendAsync<List<AccessedHost>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates users' most recently accessed hosts.
        /// </summary>
        /// <param name="parametersList">A list of update parameters</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task UpdateMostRecentlyAccessedHostsAsync(
            IEnumerable<AccessedHostsParameters> parametersList,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6c416d43-571a-454d-8350-df3e879cb33d");
            HttpContent content = new ObjectContent<IEnumerable<AccessedHostsParameters>>(parametersList, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<Guid> GetStorageKeyAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("c1d0bf9e-3220-44d9-b048-222ae15fc3e4");
            object routeValues = new { descriptor = descriptor };

            return SendAsync<Guid>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieves the default data for the authenticated user.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<User> GetUserDefaultsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a9e65880-7489-4453-aa72-0f7896f0b434");

            return SendAsync<User>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Creates a new user.
        /// </summary>
        /// <param name="userParameters">The parameters to be used for user creation.</param>
        /// <param name="createLocal"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<User> CreateUserAsync(
            CreateUserParameters userParameters,
            bool? createLocal = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("61117502-a055-422c-9122-b56e6643ed02");
            HttpContent content = new ObjectContent<CreateUserParameters>(userParameters, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (createLocal != null)
            {
                queryParams.Add("createLocal", createLocal.Value.ToString());
            }

            return SendAsync<User>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task DeleteUserAsync(
            SubjectDescriptor descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("61117502-a055-422c-9122-b56e6643ed02");
            object routeValues = new { descriptor = descriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Retrieves the data for a given user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="createIfNotExists">Whether to auto-provision the authenticated user</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<User> GetUserAsync(
            string descriptor,
            Boolean? createIfNotExists = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("61117502-a055-422c-9122-b56e6643ed02");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
            if (createIfNotExists != null)
            {
                additionalHeaders.Add("X-VSS-FaultInUser", createIfNotExists.Value.ToString());
            }

            return SendAsync<User>(
                httpMethod,
                additionalHeaders,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing user.
        /// </summary>
        /// <param name="descriptor">The identity of the user for the operation.</param>
        /// <param name="userParameters">The parameters to be used for user update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<User> UpdateUserAsync(
            string descriptor,
            UpdateUserParameters userParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("61117502-a055-422c-9122-b56e6643ed02");
            object routeValues = new { descriptor = descriptor };
            HttpContent content = new ObjectContent<UpdateUserParameters>(userParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<User>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
