using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class ApiUtil
    {        
        public static VssConnection CreateConnection(Uri serverUri, VssCredentials credentials)
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();
            settings.MaxRetryRequest = 5;
            
            var headerValues = new List<ProductInfoHeaderValue>();
            headerValues.Add(new ProductInfoHeaderValue("VstsAgent", Constants.Agent.Version));
            VssConnection connection = new VssConnection(serverUri, credentials, settings);
            return connection;
        }

        // The server only send down OAuth token in Job Request message.
        public static VssConnection GetVssConnection(JobRequestMessage jobRequest)
        {
            var serviceEndpoint = jobRequest.Environment.SystemConnection;

            if (serviceEndpoint == null ||
                serviceEndpoint.Url == null ||
                serviceEndpoint.Authorization == null ||
                string.IsNullOrEmpty(serviceEndpoint.Authorization.Scheme) ||
                serviceEndpoint.Authorization.Parameters.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(jobRequest.Environment.SystemConnection));
            }

            Uri serverUrl = serviceEndpoint.Url;
            VssCredentials credentials = null;
            string accessToken;
            if (serviceEndpoint.Authorization.Scheme == EndpointAuthorizationSchemes.OAuth &&
                serviceEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken))
            {
                credentials = new VssOAuthCredential(accessToken);
            }

            if (credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }
            else
            {
                return CreateConnection(serverUrl, credentials);
            }
        }
    }
}