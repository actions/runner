using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using GitHub.DistributedTask.WebApi;

using Sdk.WebApi.WebApi;

namespace GitHub.Services.Launch.Client
{
    public class LaunchHttpClient : RawHttpClientBase
    {

        public LaunchHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            string token,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            m_token = token;
            m_launchServiceUrl = baseUrl;
            m_formatter = new JsonMediaTypeFormatter();
        }

        // Resolve Actions
        private async Task<T> GetLaunchSignedURLResponse<R, T>(Uri uri, R request, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (HttpContent content = new ObjectContent<R>(request, m_formatter))
                {
                    requestMessage.Content = content;
                    using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                    {
                        return await ReadJsonContentAsync<T>(response, cancellationToken);
                    }
                }
            }
        }
        public async Task<ActionDownloadInfoCollection> GetResolveActionsDownloadInfoAsync(Guid planId, Guid jobId, ActionReferenceList actionReferenceList, CancellationToken cancellationToken)
        {

            var GetResolveActionsDownloadInfoURLEndpoint = new Uri(m_launchServiceUrl, $"/actions/build/{planId.ToString()}/jobs/{jobId.ToString()}/runnerresolve/actions");

            return await GetLaunchSignedURLResponse<ActionReferenceList, ActionDownloadInfoCollection>(GetResolveActionsDownloadInfoURLEndpoint, actionReferenceList, cancellationToken);
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_launchServiceUrl;
        private string m_token;
    }
}