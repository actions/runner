using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using Sdk.RSWebApi.Contracts;
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

        public async Task<AgentJobRequestMessage> GetJobMessageAsync(
            Uri requestUri,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new AcquireJobRequest
            {
                JobMessageId = messageId,
            };

            requestUri = new Uri(requestUri, "acquirejob");

            var requestContent = new ObjectContent<AcquireJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            var result = await SendAsync<AgentJobRequestMessage>(
                httpMethod,
                requestUri: requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            switch (result.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new TaskOrchestrationJobNotFoundException($"Job message not found: {messageId}");
                case HttpStatusCode.Conflict:
                    throw new TaskOrchestrationJobAlreadyAcquiredException($"Job message already acquired: {messageId}");
                default:
                    throw new Exception($"Failed to get job message: {result.Error}");
            }
        }

        public async Task CompleteJobAsync(
            Uri requestUri,
            Guid planId,
            Guid jobId,
            TaskResult result,
            Dictionary<String, VariableValue> outputs,
            IList<StepResult> stepResults,
            IList<Annotation> jobAnnotations,
            string environmentUrl,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new CompleteJobRequest()
            {
                PlanID = planId,
                JobID = jobId,
                Conclusion = result,
                Outputs = outputs,
                StepResults = stepResults,
                Annotations = jobAnnotations,
                EnvironmentUrl = environmentUrl,
            };

            requestUri = new Uri(requestUri, "completejob");

            var requestContent = new ObjectContent<CompleteJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            var response = await SendAsync(
                    httpMethod,
                    requestUri,
                    content: requestContent,
                    cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new TaskOrchestrationJobNotFoundException($"Job not found: {jobId}");
                default:
                    throw new Exception($"Failed to complete job: {response.ReasonPhrase}");
            }
        }

        public async Task<RenewJobResponse> RenewJobAsync(
            Uri requestUri,
            Guid planId,
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new RenewJobRequest()
            {
                PlanID = planId,
                JobID = jobId
            };

            requestUri = new Uri(requestUri, "renewjob");

            var requestContent = new ObjectContent<RenewJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            var result = await SendAsync<RenewJobResponse>(
                httpMethod,
                requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            switch (result.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new TaskOrchestrationJobNotFoundException($"Job not found: {jobId}");
                default:
                    throw new Exception($"Failed to renew job: {result.Error}");
            }
        }
    }
}
