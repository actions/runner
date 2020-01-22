using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.DelegatedAuthorization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Tokens.WebApi
{
    [ResourceArea(TokenOAuth2ResourceIds.AreaId)]
    public class TokenOauth2HttpClient : VssHttpClientBase
    {
        public TokenOauth2HttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenOauth2HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenOauth2HttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenOauth2HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenOauth2HttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="tokenSecretPair"></param>
        /// <param name="grantType"></param>
        /// <param name="hostId"></param>
        /// <param name="orgHostId"></param>
        /// <param name="audience"></param>
        /// <param name="redirectUri"></param>
        /// <param name="accessId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AccessTokenResult> IssueTokenAsync(
            GrantTokenSecretPair tokenSecretPair,
            GrantType grantType,
            Guid hostId,
            Guid orgHostId,
            Uri audience = null,
            Uri redirectUri = null,
            Guid? accessId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("bbc63806-e448-4e88-8c57-0af77747a323");
            HttpContent content = new ObjectContent<GrantTokenSecretPair>(tokenSecretPair, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("grantType", grantType.ToString());
            queryParams.Add("hostId", hostId.ToString());
            queryParams.Add("orgHostId", orgHostId.ToString());
            if (audience != null)
            {
                queryParams.Add("audience", audience.ToString());
            }
            if (redirectUri != null)
            {
                queryParams.Add("redirectUri", redirectUri.ToString());
            }
            if (accessId != null)
            {
                queryParams.Add("accessId", accessId.Value.ToString());
            }

            return SendAsync<AccessTokenResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(6.0, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
