using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public static class HttpClientUtilities
    {       
        public static async Task<HttpStatusCode> SendHeadCallAsync(HttpClient client, Uri uri, CancellationToken cancelToken)
        {
            using (var httpResult = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), HttpCompletionOption.ResponseHeadersRead, cancelToken).ConfigureAwait(false))
            {
                return httpResult.StatusCode;
            };            
        }
    }
}
