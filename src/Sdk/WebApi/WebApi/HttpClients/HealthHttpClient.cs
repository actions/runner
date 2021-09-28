using System;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;       

namespace GitHub.Services.Health.Client
{     
    public class HealthHttpClient : VssHttpClientBase
    {
        public HealthHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<HttpResponseMessage> GetHealthAsync()
        {
            var uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), "_apis/health"));

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await base.SendAsync(request);
            }
        }
    }
}