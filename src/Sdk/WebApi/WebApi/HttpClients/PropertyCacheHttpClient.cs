using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Identity.Client
{
    [ResourceArea(PropertyCacheResourceIds.AreaId)]
    public class PropertyCacheHttpClient : VssHttpClientBase
    {
        static PropertyCacheHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_currentApiVersion = new ApiResourceVersion(1.0);
        }

        public PropertyCacheHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public PropertyCacheHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public PropertyCacheHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public PropertyCacheHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public PropertyCacheHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }


        public async Task<string> Cache<T>(
            T value,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(PropertyCacheResourceIds.AreaName, "Cache"))
            {                
                HttpContent content = new ObjectContent<T>(value, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<string>(
                    HttpMethod.Put,
                    PropertyCacheResourceIds.PropertyCache,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        protected override IDictionary<String, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private static Dictionary<String, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;
    }
}
