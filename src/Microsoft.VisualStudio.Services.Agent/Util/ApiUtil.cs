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

        //This code is copied from Ting PR, to enable E2E test
        public static VssConnection GetVssConnection(JobRequestMessage jobRequest)
        {
            var serviceEndpoint = jobRequest.Environment.SystemConnection;

            Uri serverUrl = null;
            VssCredentials credentials = null;
            if (serviceEndpoint != null)
            {
                serverUrl = serviceEndpoint.Url;

                String accessToken = GetAccessToken(serviceEndpoint.Authorization);
                if (!String.IsNullOrEmpty(accessToken))
                {
                    switch (serviceEndpoint.Authorization.Scheme)
                    {
                        case EndpointAuthorizationSchemes.OAuth:
                            credentials = new VssOAuthCredential(accessToken);
                            break;

                        case EndpointAuthorizationSchemes.OAuthWrap:
                            credentials = new VssServiceIdentityCredential(new VssServiceIdentityToken(accessToken));
                            break;
                    }
                }
            }

            if (serverUrl != null && credentials != null)
            {
                return CreateConnection(serverUrl, credentials);
            }
            else
            {
                return null;
            }
        }

        private static String GetAccessToken(EndpointAuthorization authorization)
        {
            if (authorization == null || String.IsNullOrEmpty(authorization.Scheme) || authorization.Parameters.Count == 0)
            {
                return null;
            }

            String accessToken;
            switch (authorization.Scheme)
            {
                case EndpointAuthorizationSchemes.OAuth:
                case EndpointAuthorizationSchemes.OAuthWrap:
                    if (authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken))
                    {
                        return accessToken;
                    }

                    break;
                // TODO: add more authorization types
                default:
                    // Unable to Construct credentials, return null.
                    break;
            }

            return null;
        }
    }
}