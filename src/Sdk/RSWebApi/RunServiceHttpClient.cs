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
using Newtonsoft.Json;
using Sdk.RSWebApi.Contracts;
using Sdk.WebApi.WebApi;

namespace GitHub.Actions.RunService.WebApi
{
    public class RunServiceHttpClient : RawHttpClientBase
    {
        private static readonly JsonSerializerSettings s_serializerSettings;

        static RunServiceHttpClient()
        {
            s_serializerSettings = new VssJsonMediaTypeFormatter().SerializerSettings;
            s_serializerSettings.DateParseHandling = DateParseHandling.None;
            s_serializerSettings.FloatParseHandling = FloatParseHandling.Double;
        }

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
            string runnerOS,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            var payload = new AcquireJobRequest
            {
                JobMessageId = messageId,
                RunnerOS = runnerOS
            };

            requestUri = new Uri(requestUri, "acquirejob");

            var requestContent = new ObjectContent<AcquireJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            var result = await SendAsync<AgentJobRequestMessage>(
                httpMethod,
                requestUri: requestUri,
                content: requestContent,
                readErrorBody: true,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            if (TryParseErrorBody(result.ErrorBody, out RunServiceError error))
            {
                switch ((HttpStatusCode)error.Code)
                {
                    case HttpStatusCode.NotFound:
                        throw new TaskOrchestrationJobNotFoundException($"Job message not found '{messageId}'. {error.Message}");
                    case HttpStatusCode.Conflict:
                        throw new TaskOrchestrationJobAlreadyAcquiredException($"Job message already acquired '{messageId}'. {error.Message}");
                    case HttpStatusCode.UnprocessableEntity:
                        throw new TaskOrchestrationJobUnprocessableException($"Unprocessable job '{messageId}'. {error.Message}");
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorBody))
            {
                throw new Exception($"Failed to get job message: {result.Error}. {Truncate(result.ErrorBody)}");
            }
            else
            {
                throw new Exception($"Failed to get job message: {result.Error}");
            }
        }

        public async Task CompleteJobAsync(
            Uri requestUri,
            Guid planId,
            Guid jobId,
            TaskResult conclusion,
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
                Conclusion = conclusion,
                Outputs = outputs,
                StepResults = stepResults,
                Annotations = jobAnnotations,
                EnvironmentUrl = environmentUrl,
            };

            requestUri = new Uri(requestUri, "completejob");

            var requestContent = new ObjectContent<CompleteJobRequest>(payload, new VssJsonMediaTypeFormatter(true));
            var result = await Send2Async(
                    httpMethod,
                    requestUri,
                    content: requestContent,
                    cancellationToken: cancellationToken);
            if (result.IsSuccess)
            {
                return;
            }

            if (TryParseErrorBody(result.ErrorBody, out RunServiceError error))
            {
                switch ((HttpStatusCode)error.Code)
                {
                    case HttpStatusCode.NotFound:
                        throw new TaskOrchestrationJobNotFoundException($"Job not found: {jobId}. {error.Message}");
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorBody))
            {
                throw new Exception($"Failed to complete job: {result.Error}. {Truncate(result.ErrorBody)}");
            }
            else
            {
                throw new Exception($"Failed to complete job: {result.Error}");
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
                readErrorBody: true,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            if (TryParseErrorBody(result.ErrorBody, out RunServiceError error))
            {
                switch ((HttpStatusCode)error.Code)
                {
                    case HttpStatusCode.NotFound:
                        throw new TaskOrchestrationJobNotFoundException($"Job not found: {jobId}. {error.Message}");
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorBody))
            {
                throw new Exception($"Failed to renew job: {result.Error}. {Truncate(result.ErrorBody)}");
            }
            else
            {
                throw new Exception($"Failed to renew job: {result.Error}");
            }
        }

        protected override async Task<T> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json, s_serializerSettings);
        }

        private static bool TryParseErrorBody(string errorBody, out RunServiceError error)
        {
            if (!string.IsNullOrEmpty(errorBody))
            {
                try
                {
                    error = JsonUtility.FromString<RunServiceError>(errorBody);
                    if (error?.Source == "actions-run-service")
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            error = null;
            return false;
        }

        private static string Truncate(string errorBody)
        {
            if (errorBody.Length > 100)
            {
                return errorBody.Substring(0, 100) + "[truncated]";
            }

            return errorBody;
        }
    }
}
