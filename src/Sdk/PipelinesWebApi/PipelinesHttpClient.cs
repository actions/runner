using System;
using System.Net.Http;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Actions.Pipelines.WebApi
{
    [ResourceArea(PipelinesArea.IdString)]
    public class PipelinesHttpClient : PipelinesHttpClientBase
    {
        public PipelinesHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public PipelinesHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public PipelinesHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public PipelinesHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public PipelinesHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
    }
}
