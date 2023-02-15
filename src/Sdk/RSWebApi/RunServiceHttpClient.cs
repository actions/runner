using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using Sdk.WebApi.WebApi;

namespace GitHub.Actions.RunService.WebApi
{
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

        public Task<AgentJobRequestMessage> GetJobMessageAsync(
            Uri requestUri,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new AcquireJobRequest
            {
                StreamID = messageId
            };

            requestUri = new Uri(requestUri, "acquirejob");

            var requestContent = new ObjectContent<AcquireJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            return SendAsync<AgentJobRequestMessage>(
                httpMethod,
                requestUri: requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);
        }

        public Task CompleteJobAsync(
            Uri requestUri,
            Guid planId,
            Guid jobId,
            TaskResult result,
            Dictionary<String, VariableValue> outputs,
            IList<StepResult> stepResults,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new CompleteJobRequest()
            {
                PlanID = planId,
                JobID = jobId,
                Conclusion = result,
                Outputs = outputs,
                StepResults = stepResults
            };

            requestUri = new Uri(requestUri, "completejob");

            var requestContent = new ObjectContent<CompleteJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            return SendAsync(
                httpMethod,
                requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);
        }
    }
}
