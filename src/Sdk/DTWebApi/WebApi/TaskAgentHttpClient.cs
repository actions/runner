using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public class TaskAgentHttpClient : TaskAgentHttpClientBase
    {
        public TaskAgentHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskAgentHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskAgentHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskAgentHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskAgentHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public Task<TaskAgentJobRequest> FinishAgentRequestAsync(
            Int32 poolId,
            Int64 requestId,
            Guid lockToken,
            DateTime finishTime,
            TaskResult result,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new TaskAgentJobRequest
            {
                RequestId = requestId,
                FinishTime = finishTime,
                Result = result,
            };

            return UpdateAgentRequestAsync(poolId, requestId, lockToken, request, userState, cancellationToken);
        }

        public Task<List<TaskAgent>> GetAgentsAsync(
            int poolId,
            string agentName = null,
            bool? includeCapabilities = null,
            bool? includeAssignedRequest = null,
            bool? includeLastCompletedRequest = null,
            IEnumerable<string> propertyFilters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAgentsAsync(poolId, agentName, includeCapabilities, includeAssignedRequest, includeLastCompletedRequest, propertyFilters, null, userState, cancellationToken);
        }

        public Task<TaskAgentJobRequest> RenewAgentRequestAsync(
            Int32 poolId,
            Int64 requestId,
            Guid lockToken,
            DateTime? expiresOn = null,
            string orchestrationId = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new TaskAgentJobRequest
            {
                RequestId = requestId,
                LockedUntil = expiresOn,
            };

            var additionalHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(orchestrationId))
            {
                additionalHeaders["X-VSS-OrchestrationId"] = orchestrationId;
            }

            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId, requestId = requestId };
            HttpContent content = new ObjectContent<TaskAgentJobRequest>(request, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("lockToken", lockToken.ToString());

            return SendAsync<TaskAgentJobRequest>(
                httpMethod,
                additionalHeaders,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        public Task<TaskAgent> ReplaceAgentAsync(
            Int32 poolId,
            TaskAgent agent,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(agent, "agent");
            return ReplaceAgentAsync(poolId, agent.Id, agent, userState, cancellationToken);
        }

        protected Task<T> SendAsync<T>(
            HttpMethod method,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Func<HttpResponseMessage, CancellationToken, Task<T>> processResponse = null)
        {
            return SendAsync<T>(method, null, locationId, routeValues, version, content, queryParameters, userState, cancellationToken, processResponse);
        }

        protected async Task<T> SendAsync<T>(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Func<HttpResponseMessage, CancellationToken, Task<T>> processResponse = null)
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(method, additionalHeaders, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false))
            {
                return await SendAsync<T>(requestMessage, userState, cancellationToken, processResponse).ConfigureAwait(false);
            }
        }

        protected async Task<T> SendAsync<T>(
            HttpRequestMessage message,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Func<HttpResponseMessage, CancellationToken, Task<T>> processResponse = null)
        {
            if (processResponse == null)
            {
                processResponse = ReadContentAsAsync<T>;
            }

            //ConfigureAwait(false) enables the continuation to be run outside
            //any captured SyncronizationContext (such as ASP.NET's) which keeps things
            //from deadlocking...
            using (HttpResponseMessage response = await this.SendAsync(message, userState, cancellationToken).ConfigureAwait(false))
            {
                return await processResponse(response, cancellationToken).ConfigureAwait(false);
            }
        }

        private readonly ApiResourceVersion m_currentApiVersion = new ApiResourceVersion(3.0, 1);
    }
}
