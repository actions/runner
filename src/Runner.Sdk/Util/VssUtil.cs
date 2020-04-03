using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.OAuth;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Net;

namespace GitHub.Runner.Sdk
{
    public static class VssUtil
    {
        public static void InitializeVssClientSettings(List<ProductInfoHeaderValue> additionalUserAgents, IWebProxy proxy)
        {
            var headerValues = new List<ProductInfoHeaderValue>();
            headerValues.AddRange(additionalUserAgents);
            headerValues.Add(new ProductInfoHeaderValue($"({RuntimeInformation.OSDescription.Trim()})"));

            if (VssClientHttpRequestSettings.Default.UserAgent != null && VssClientHttpRequestSettings.Default.UserAgent.Count > 0)
            {
                headerValues.AddRange(VssClientHttpRequestSettings.Default.UserAgent);
            }

            VssClientHttpRequestSettings.Default.UserAgent = headerValues;
            VssHttpMessageHandler.DefaultWebProxy = proxy;
        }

        public static VssConnection CreateConnection(Uri serverUri, VssCredentials credentials, IEnumerable<DelegatingHandler> additionalDelegatingHandler = null, TimeSpan? timeout = null)
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();

            int maxRetryRequest;
            if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY") ?? string.Empty, out maxRetryRequest))
            {
                maxRetryRequest = 3;
            }

            // make sure MaxRetryRequest in range [3, 10]
            settings.MaxRetryRequest = Math.Min(Math.Max(maxRetryRequest, 3), 10);

            if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT") ?? string.Empty, out int httpRequestTimeoutSeconds))
            {
                settings.SendTimeout = timeout ?? TimeSpan.FromSeconds(100);
            }
            else
            {
                // prefer environment variable
                settings.SendTimeout = TimeSpan.FromSeconds(Math.Min(Math.Max(httpRequestTimeoutSeconds, 100), 1200));
            }


            // Remove Invariant from the list of accepted languages.
            //
            // The constructor of VssHttpRequestSettings (base class of VssClientHttpRequestSettings) adds the current
            // UI culture to the list of accepted languages. The UI culture will be Invariant on OSX/Linux when the
            // LANG environment variable is not set when the program starts. If Invariant is in the list of accepted
            // languages, then "System.ArgumentException: The value cannot be null or empty." will be thrown when the
            // settings are applied to an HttpRequestMessage.
            settings.AcceptLanguages.Remove(CultureInfo.InvariantCulture);

            VssConnection connection = new VssConnection(serverUri, new VssHttpMessageHandler(credentials, settings), additionalDelegatingHandler);
            return connection;
        }

        public static VssCredentials GetVssCredential(ServiceEndpoint serviceEndpoint)
        {
            ArgUtil.NotNull(serviceEndpoint, nameof(serviceEndpoint));
            ArgUtil.NotNull(serviceEndpoint.Authorization, nameof(serviceEndpoint.Authorization));
            ArgUtil.NotNullOrEmpty(serviceEndpoint.Authorization.Scheme, nameof(serviceEndpoint.Authorization.Scheme));

            if (serviceEndpoint.Authorization.Parameters.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(serviceEndpoint));
            }

            VssCredentials credentials = null;
            string accessToken;
            if (serviceEndpoint.Authorization.Scheme == EndpointAuthorizationSchemes.OAuth &&
                serviceEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken))
            {
                credentials = new VssCredentials(new VssOAuthAccessTokenCredential(accessToken), CredentialPromptType.DoNotPrompt);
            }

            return credentials;
        }
    }
}
