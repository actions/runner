using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Users.Client
{
    [ResourceArea(UserResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 100, failurePercentage: 80, MaxConcurrentRequests = 40)]
    public class UserHttpClient : UserHttpClientBase
    {
        public UserHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Deletes an attribute for the given user.
        /// </summary>
        /// <param name="attributeName">Name of the attribute to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task DeleteSelfAttributeAsync(
            string attributeName,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.DeleteAttributeAsync(UserRestApiConstants.Me, attributeName, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieves an attribute for a given user.
        /// </summary>
        /// <param name="attributeName">The name of the attribute to retrieve.  If not provided, all attributes are returned.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<UserAttribute> GetSelfAttributeAsync(
            string attributeName,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(attributeName, nameof(attributeName));

            return base.GetAttributeAsync(UserRestApiConstants.Me, attributeName, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="queryPattern"></param>
        /// <param name="modifiedAfter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<UserAttributes> QuerySelfAttributesAsync(
            string continuationToken,
            string queryPattern = null,
            DateTimeOffset? modifiedAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.QueryAttributesAsync(UserRestApiConstants.Me, continuationToken, queryPattern, modifiedAfter, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates multiple attributes for a given user.
        /// </summary>
        /// <param name="attributeParametersList">The list of resource representations containing the attribute data to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<UserAttribute>> SetSelfAttributesAsync(
            IEnumerable<SetUserAttributeParameters> attributeParametersList,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SetAttributesAsync(UserRestApiConstants.Me, attributeParametersList, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieves the data for a given user.
        /// </summary>
        /// <param name="descriptor">The descriptor identifying the user for the operation.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<User> GetSelfAsync(
            object userState = null,
            bool? createIfNotExists = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetUserAsync(UserRestApiConstants.Me, createIfNotExists, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing user.
        /// </summary>
        /// <param name="userParameters">The object containing the user's data to be updated.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<User> UpdateSelfAsync(
            UpdateUserParameters userParameters,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.UpdateUserAsync(UserRestApiConstants.Me, userParameters, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieves the user avatar.
        /// </summary>
        /// <param name="descriptor">The descriptor identifying the user for the operation.</param>
        /// <param name="size"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Avatar> GetSelfAvatarAsync(
            AvatarSize? size = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetAvatarAsync(UserRestApiConstants.Me, size, null, userState, cancellationToken);
        }
    }
}
