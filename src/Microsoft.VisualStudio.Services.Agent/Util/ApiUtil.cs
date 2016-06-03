using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class ApiUtil
    {
        public static VssConnection CreateConnection(Uri serverUri, VssCredentials credentials)
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();
            settings.MaxRetryRequest = 5;

            var headerValues = new List<ProductInfoHeaderValue>();
#if OS_WINDOWS
            headerValues.Add(new ProductInfoHeaderValue("VstsAgentCore-windows", Constants.Agent.Version));
#elif OS_OSX
            headerValues.Add(new ProductInfoHeaderValue("VstsAgentCore-darwin", Constants.Agent.Version));		
#elif OS_LINUX
            headerValues.Add(new ProductInfoHeaderValue("VstsAgentCore-linux", Constants.Agent.Version));		
#endif
            headerValues.Add(new ProductInfoHeaderValue($"({RuntimeInformation.OSDescription.Trim()})"));

            if (settings.UserAgent != null && settings.UserAgent.Count > 0)
            {
                headerValues.AddRange(settings.UserAgent);
            }

            settings.UserAgent = headerValues;

            VssConnection connection = new VssConnection(serverUri, credentials, settings);
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
                //TODO: consume the new Microsoft.VisualStudio.Services.OAuth.VssOAuthAccessTokenCredential
                //when it is available in the rest SDK
#pragma warning disable 618
                credentials = new VssOAuthCredential(accessToken);
#pragma warning restore 618
            }

            return credentials;
        }
    }
}