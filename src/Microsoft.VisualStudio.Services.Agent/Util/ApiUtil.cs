using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class ApiUtil
    {
        public static void InitializeVssClientSettings(IVstsAgentWebProxy proxySetting, IAgentCertificateManager certSetting)
        {
            var headerValues = new List<ProductInfoHeaderValue>();
            headerValues.Add(new ProductInfoHeaderValue($"VstsAgentCore-{BuildConstants.AgentPackage.PackageName}", Constants.Agent.Version));
            headerValues.Add(new ProductInfoHeaderValue($"({RuntimeInformation.OSDescription.Trim()})"));

            if (VssClientHttpRequestSettings.Default.UserAgent != null && VssClientHttpRequestSettings.Default.UserAgent.Count > 0)
            {
                headerValues.AddRange(VssClientHttpRequestSettings.Default.UserAgent);
            }

            VssClientHttpRequestSettings.Default.UserAgent = headerValues;
            VssClientHttpRequestSettings.Default.ClientCertificateManager = certSetting;
            VssHttpMessageHandler.DefaultWebProxy = proxySetting;
        }

        public static VssConnection CreateConnection(Uri serverUri, VssCredentials credentials, IEnumerable<DelegatingHandler> additionalDelegatingHandler = null)
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();

            int maxRetryRequest;
            if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_HTTP_RETRY") ?? string.Empty, out maxRetryRequest))
            {
                maxRetryRequest = 5;
            }

            // make sure MaxRetryRequest in range [5, 10]
            settings.MaxRetryRequest = Math.Min(Math.Max(maxRetryRequest, 5), 10);

            int httpRequestTimeoutSeconds;
            if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_HTTP_TIMEOUT") ?? string.Empty, out httpRequestTimeoutSeconds))
            {
                httpRequestTimeoutSeconds = 100;
            }

            // make sure httpRequestTimeoutSeconds in range [100, 1200]
            settings.SendTimeout = TimeSpan.FromSeconds(Math.Min(Math.Max(httpRequestTimeoutSeconds, 100), 1200));

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

        // The server only send down OAuth token in Job Request message.
        public static VssConnection GetVssConnection(JobRequestMessage jobRequest)
        {
            ArgUtil.NotNull(jobRequest, nameof(jobRequest));
            ArgUtil.NotNull(jobRequest.Environment, nameof(jobRequest.Environment));
            ArgUtil.NotNull(jobRequest.Environment.SystemConnection, nameof(jobRequest.Environment.SystemConnection));
            ArgUtil.NotNull(jobRequest.Environment.SystemConnection.Url, nameof(jobRequest.Environment.SystemConnection.Url));

            Uri serverUrl = jobRequest.Environment.SystemConnection.Url;
            var credentials = GetVssCredential(jobRequest.Environment.SystemConnection);

            if (credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }
            else
            {
                return CreateConnection(serverUrl, credentials);
            }
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
                credentials = new VssCredentials(null, new VssOAuthAccessTokenCredential(accessToken), CredentialPromptType.DoNotPrompt);
            }

            return credentials;
        }

        public static PlanFeatures GetFeatures(TaskOrchestrationPlanReference plan)
        {
            ArgUtil.NotNull(plan, nameof(plan));
            PlanFeatures features = PlanFeatures.None;
            if (plan.Version >= 8)
            {
                features |= PlanFeatures.JobCompletedPlanEvent;
            }

            return features;
        }
    }

    [Flags]
    public enum PlanFeatures
    {
        None = 0,
        JobCompletedPlanEvent = 1,
    }
}