using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.TokenSigningKeyLifecycle.Client
{
    [ResourceArea(TokenSigningKeyLifecycleResourceIds.AreaId)]
    public class TokenSigningKeyHttpClient : VssHttpClientBase
    {
        static TokenSigningKeyHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_currentApiVersion = new ApiResourceVersion(1.0);
        }

        public TokenSigningKeyHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenSigningKeyHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenSigningKeyHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenSigningKeyHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenSigningKeyHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<TokenSigningKey> GetSigningKeys(
            string signingKeyNamespaceName,
            int keyId,
            Object userState = null, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenSigningKeyLifecycleResourceIds.AreaName, "GetValidationSigningKeys"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("namespaceName", signingKeyNamespaceName);
                queryParameters.Add("keyId", keyId.ToString());

                return await SendAsync<TokenSigningKey>(
                    HttpMethod.Get,
                    TokenSigningKeyLifecycleResourceIds.SigningKeysLocationId,
                    queryParameters: queryParameters,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<TokenSigningKeyNamespace> GetNamespace(
            string signingKeyNamespaceName,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenSigningKeyLifecycleResourceIds.AreaName, "GetNamespace"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("namespaceName", signingKeyNamespaceName);

                return await SendAsync<TokenSigningKeyNamespace>(
                    HttpMethod.Get,
                    TokenSigningKeyLifecycleResourceIds.NamespaceLocationId,
                    queryParameters: queryParameters,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private static Dictionary<string, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;
    }
}
