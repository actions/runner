using System;
using System.Net.Http;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Identity.Mru.Client
{
    [ResourceArea(IdentityMruResourceIds.AreaId)]
    public class IdentityMruHttpClient : IdentityMruHttpClientBase
    {
        public IdentityMruHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public IdentityMruHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public IdentityMruHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public IdentityMruHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public IdentityMruHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
    }
}
