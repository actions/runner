using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using Sdk.WebApi.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public class RunServiceHttpClient : RawHttpClientBase
    {
        public RunServiceHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials)
            : base(baseUrl, credentials)
        {
        }

        public RunServiceHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public RunServiceHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public RunServiceHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public RunServiceHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public Task<Pipelines.AgentJobRequestMessage> GetJobMessageAsync(
            Uri requestUri,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new {
                StreamID = messageId
            };

            requestUri = new Uri(requestUri, "acquirejob");

            var payloadJson = JsonUtility.ToString(payload);
            var requestContent = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            return SendAsync<Pipelines.AgentJobRequestMessage>(
                httpMethod,
                requestUri: requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);
        }

        public Task CompleteJobAsync(
            Uri requestUri,
            Guid planId,
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new {
                PlanId = planId,
                JobId = jobId
            };

            requestUri = new Uri(requestUri, "completejob");

            var payloadJson = JsonUtility.ToString(payload);
            var requestContent = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            return SendAsync(
                httpMethod,
                requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);
        }
    }
}
