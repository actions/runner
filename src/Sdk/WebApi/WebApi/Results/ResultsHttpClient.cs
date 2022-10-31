using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Results.Contracts;
using GitHub.Services.WebApi.Utilities.Internal;
using System.Net.Http.Formatting;

namespace GitHub.Services.Results.Client
{
    public class ResultsHttpClient
    {
        
        public ResultsHttpClient(string token, string resultsServiceUrl)
        {
            m_token = token;
            m_resultsServiceUrl = resultsServiceUrl;
            m_formatter = new JsonMediaTypeFormatter();
            m_client = new HttpClient();
        }

        public async Task<StepSummaryUploadUrlResponse> GetStepSummaryUploadUrlAsync(string jobId, string planId, string stepId, CancellationToken cancellationToken)
        {
            var request = new StepSummaryUploadUrlRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId
            };

            var stepSummaryUploadRequest = m_resultsServiceUrl+"/GetStepSummarySignedBlobURL";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadRequest);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            HttpContent content = new ObjectContent<StepSummaryUploadUrlRequest>(request, m_formatter);
            requestMessage.Content = content;

            var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken);
            return await ReadJsonContentAsync<StepSummaryUploadUrlResponse>(response, cancellationToken);
        }

        public async Task<StepSummaryUploadCompleteResponse> StepSummaryUploadCompleteAsync(string jobId, string planId, string stepId, Int64 size, CancellationToken cancellationToken)
        {
            var request = new StepSummaryUploadCompleteRequest()
            {
                WorkflowJobRunBackendId = jobId,
                WorkflowRunBackendId = planId,
                StepBackendId = stepId,
                Size = size,
                UploadedAt = DateTime.Now.ToFileTime().ToString()
            };

            var stepSummaryUploadCompleteRequest = m_resultsServiceUrl+"/StepSummaryUploadComplete";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, stepSummaryUploadCompleteRequest);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_token);
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            HttpContent content = new ObjectContent<StepSummaryUploadCompleteRequest>(request, m_formatter);
            requestMessage.Content = content;

            var response = await SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken);
            return await ReadJsonContentAsync<StepSummaryUploadCompleteResponse>(response, cancellationToken);
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage message,
            HttpCompletionOption completionOption,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message.Headers.UserAgent != null)
            {
                foreach (ProductInfoHeaderValue headerValue in UserAgentUtility.GetDefaultRestUserAgent())
                {
                    if (!message.Headers.UserAgent.Contains(headerValue))
                    {
                        message.Headers.UserAgent.Add(headerValue);
                    }
                }
            }

            HttpResponseMessage response = await m_client.SendAsync(message, completionOption, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        protected virtual async Task<T> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await response.Content.ReadAsAsync<T>(new[] { m_formatter }, cancellationToken).ConfigureAwait(false);
        }

        // private static Dictionary<String, Type> s_translatedExceptions;
        // private const String connectSubUrl = "_apis/connectionData";
        // protected static readonly ApiResourceVersion s_currentApiVersion = new ApiResourceVersion(1.0);
        private string m_token;
        private string m_resultsServiceUrl;
        private MediaTypeFormatter m_formatter;
        private HttpClient m_client;
    }
}
