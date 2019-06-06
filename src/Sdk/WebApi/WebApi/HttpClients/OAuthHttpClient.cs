using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.OAuth.Client
{
    public class OAuthHttpClient : VssHttpClientBase
    {
        public OAuthHttpClient(
            Uri baseUrl, 
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

         public OAuthHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OAuthHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OAuthHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OAuthHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// There are 3 error response kinds:
        ///   1. An authorization error with no redirect URL.
        ///   2. An authorization error with redirect URL.
        ///   3. A token request error.
        ///   
        /// If the status code is not 302, we will throw an exception with the response body's content
        /// read as a string, which contains the specific error code.
        /// 
        /// http://tools.ietf.org/html/rfc6749#section-4.1.2
        /// http://tools.ietf.org/html/rfc6749#section-5.2
        /// </summary>
        /// <param name="response"></param>
        protected override Task HandleResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode != HttpStatusCode.Found)
            {
                return base.HandleResponseAsync(response, cancellationToken);
            }
            else
            {
                return Task.FromResult(false);
            }
        }        

        public Task<AuthorizationResponse> AuthorizeAsync(
            String clientId,
            String responseType,
            String redirectUri,
            String scope,
            String state,
            Object userState, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty("clientId", clientId);
            ArgumentUtility.CheckStringForNullOrEmpty("responseType", responseType);

            List<KeyValuePair<String, String>> parameters = new List<KeyValuePair<String, String>>();
            parameters.Add(new KeyValuePair<String, String>("client_id", clientId));
            parameters.Add(new KeyValuePair<String, String>("response_type", responseType));

            if (!String.IsNullOrEmpty(redirectUri))
            {
                parameters.Add(new KeyValuePair<String, String>("redirect_uri", redirectUri));
            }

            if (!String.IsNullOrEmpty(scope))
            {
                parameters.Add(new KeyValuePair<String, String>("scope", scope));
            }

            if (!String.IsNullOrEmpty(state))
            {
                parameters.Add(new KeyValuePair<String, String>("state", state));
            }

            Uri uri = new Uri(BaseAddress, String.Format(CultureInfo.InvariantCulture, "/_apis/OAuth/Auth"));
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
            message.Content = new FormUrlEncodedContent(parameters);

            return SendAsync<AuthorizationResponse>(message, userState, cancellationToken);
        }

        public Task<AccessTokenResponse> CreateTokenAsync(
            String grantType,
            String code,
            String refreshToken,
            String scope,
            String redirectUri,
            Object userState, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty("grantType", grantType);

            List<KeyValuePair<String, String>> parameters = new List<KeyValuePair<String, String>>();
            parameters.Add(new KeyValuePair<String, String>("grant_type", grantType));

            if (!String.IsNullOrEmpty(code))
            {
                parameters.Add(new KeyValuePair<String, String>("code", code));
            }

            if (!String.IsNullOrEmpty(refreshToken))
            {
                parameters.Add(new KeyValuePair<String, String>("refresh_token", refreshToken));
            }

            if (!String.IsNullOrEmpty(scope))
            {
                parameters.Add(new KeyValuePair<String, String>("scope", scope));
            }

            if (!String.IsNullOrEmpty(redirectUri))
            {
                parameters.Add(new KeyValuePair<String, String>("redirect_uri", redirectUri));
            }

            Uri uri = new Uri(BaseAddress, String.Format(CultureInfo.InvariantCulture, "/_apis/OAuth/Tokens"));
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
            message.Content = new FormUrlEncodedContent(parameters);

            return SendAsync<AccessTokenResponse>(message, userState, cancellationToken);
        }
    }
}
