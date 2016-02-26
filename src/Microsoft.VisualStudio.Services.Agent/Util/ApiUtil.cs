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
            headerValues.Add(new ProductInfoHeaderValue("VstsAgent", AgentService.Version));
            VssConnection connection = new VssConnection(serverUri, credentials, settings);
            return connection;
            //connection.ConnectAsync().SyncResult();
        }
    }
}