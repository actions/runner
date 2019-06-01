using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Servicing.Client
{
    /// <summary>
    /// Http client for the /_apis/servicelevel REST endpoint.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ServiceLevelHttpClient : VssHttpClientBase
    {
        public ServiceLevelHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ServiceLevelHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ServiceLevelHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ServiceLevelHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ServiceLevelHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<ServiceLevelData> GetServiceLevelAsync()
        {
            using (new OperationScope(ServicingResourceIds.AreaName, "GetServiceLevel"))
            {
                Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), "/_apis/servicelevel"));
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri);
                return await base.SendAsync<ServiceLevelData>(message);
            }
        }
    }
}
