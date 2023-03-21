using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Results.Contracts;
using System.Net.Http.Formatting;
using Sdk.WebApi.WebApi;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Services.Results.Client
{
    public class BrokerHttpClient : RawHttpClientBase
    {
        public BrokerHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            string token,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            m_token = token;
            m_brokerUrl = baseUrl;
            m_formatter = new JsonMediaTypeFormatter();
        }

        public async Task<TaskAgentMessage> GetMessagesAsync(CancellationToken cancellationToken)
        {
            var uri = new Uri(m_brokerUrl, Constants.Messages);
            return await GetSignedURLResponse<TaskAgentMessage>(uri, cancellationToken);
        }

        // Get Sas URL calls
        private async Task<T> GetSignedURLResponse<T>(Uri uri, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
                requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                using (var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
                {
                    return await ReadJsonContentAsync<T>(response, cancellationToken);
                }
            }
        }

        private MediaTypeFormatter m_formatter;
        private Uri m_brokerUrl;
        private string m_token;
    }

    // Constants specific to results
    public static class Constants
    {

        public static readonly string Messages = "messages";
    }

}
