using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.WebApi;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides a method for exchanging tokens with a secure token service which supports OAuth 2.0.
    /// </summary>
    public class VssOAuthTokenHttpClient
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenHttpClient</c> using the specified authorization URL as the token 
        /// exchange endpoint. Requests for tokens will be posted to the provided URL.
        /// </summary>
        /// <param name="authorizationUrl">The endpoint used to acquire new tokens from the secure token service</param>
        public VssOAuthTokenHttpClient(Uri authorizationUrl)
        {
            ArgumentUtility.CheckForNull(authorizationUrl, nameof(authorizationUrl));
            m_authorizationUrl = authorizationUrl;
            m_formatter = new VssJsonMediaTypeFormatter();
        }

        /// <summary>
        /// Gets the authorization URL for the secure token service.
        /// </summary>
        public Uri AuthorizationUrl
        {
            get
            {
                return m_authorizationUrl;
            }
        }

        /// <summary>
        /// Performs a token exchange using the specified authorization grant and client credentials.
        /// </summary>
        /// <param name="grant">The authorization grant for the token request</param>
        /// <param name="credential">The credentials to present to the secure token service as proof of identity</param>
        /// <param name="tokenParameters">An collection of additional parameters to provide for the token request</param>
        /// <param name="cancellationToken">A token for signalling cancellation</param>
        /// <returns>A <c>Task&lt;VssOAuthTokenResponse&gt;</c> which may be used to track progress of the token request</returns>
        public async Task<VssOAuthTokenResponse> GetTokenAsync(
            VssOAuthGrant grant,
            VssOAuthClientCredential credential,
            VssOAuthTokenParameters tokenParameters = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            VssTraceActivity traceActivity = VssTraceActivity.Current;
            using (HttpClient client = new HttpClient(CreateMessageHandler(this.AuthorizationUrl)))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, this.AuthorizationUrl);
                requestMessage.Content = CreateRequestContent(grant, credential, tokenParameters);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (VssClientHttpRequestSettings.Default.UseHttp11)
                {
                    requestMessage.Version = HttpVersion.Version11;
                }

                foreach (var headerVal in VssClientHttpRequestSettings.Default.UserAgent)
                {
                    if (!requestMessage.Headers.UserAgent.Contains(headerVal))
                    {
                        requestMessage.Headers.UserAgent.Add(headerVal);
                    }
                }

                using (var response = await client.SendAsync(requestMessage, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    string correlationId = "Unknown";
                    if (response.Headers.TryGetValues("x-ms-request-id", out IEnumerable<string> requestIds))
                    {
                        correlationId = string.Join(",", requestIds);
                    }
                    VssHttpEventSource.Log.AADCorrelationID(correlationId);

                    if (IsValidTokenResponse(response))
                    {
                        return await response.Content.ReadAsAsync<VssOAuthTokenResponse>(new[] { m_formatter }, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new VssServiceResponseException(response.StatusCode, responseContent, null);
                    }
                }
            }
        }

        private static Boolean IsValidTokenResponse(HttpResponseMessage response)
        {
            return response.StatusCode == HttpStatusCode.OK || (response.StatusCode == HttpStatusCode.BadRequest && IsJsonResponse(response));
        }

        private static HttpMessageHandler CreateMessageHandler(Uri requestUri)
        {
            var retryOptions = new VssHttpRetryOptions()
            {
                RetryableStatusCodes =
                {
                    HttpStatusCode.InternalServerError,
                    VssNetworkHelper.TooManyRequests,
                },
            };

            HttpClientHandler messageHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = false
            };

            // Inherit proxy setting from VssHttpMessageHandler
            if (VssHttpMessageHandler.DefaultWebProxy != null)
            {
                messageHandler.Proxy = VssHttpMessageHandler.DefaultWebProxy;
                messageHandler.UseProxy = true;
            }

            if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                VssClientHttpRequestSettings.Default.ClientCertificateManager != null &&
                VssClientHttpRequestSettings.Default.ClientCertificateManager.ClientCertificates != null &&
                VssClientHttpRequestSettings.Default.ClientCertificateManager.ClientCertificates.Count > 0)
            {
                messageHandler.ClientCertificates.AddRange(VssClientHttpRequestSettings.Default.ClientCertificateManager.ClientCertificates);
            }

            if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback != null)
            {
                messageHandler.ServerCertificateCustomValidationCallback = VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback;
            }

            return new VssHttpRetryMessageHandler(retryOptions, messageHandler);
        }

        private static HttpContent CreateRequestContent(params IVssOAuthTokenParameterProvider[] parameterProviders)
        {
            var parameters = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameterProvider in parameterProviders)
            {
                if (parameterProvider != null)
                {
                    parameterProvider.SetParameters(parameters);
                }
            }

            return new FormUrlEncodedContent(parameters);
        }

        private static Boolean HasContent(HttpResponseMessage response)
        {
            if (response != null &&
                response.StatusCode != HttpStatusCode.NoContent &&
                response.Content != null &&
                response.Content.Headers != null &&
                response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength != 0)
            {
                return true;
            }

            return false;
        }

        private static Boolean IsJsonResponse(HttpResponseMessage response)
        {
            if (HasContent(response) &&
                response.Content.Headers != null &&
                response.Content.Headers.ContentType != null &&
                !String.IsNullOrEmpty(response.Content.Headers.ContentType.MediaType))
            {
                return String.Equals("application/json", response.Content.Headers.ContentType.MediaType, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private readonly Uri m_authorizationUrl;
        private readonly MediaTypeFormatter m_formatter;
    }
}
