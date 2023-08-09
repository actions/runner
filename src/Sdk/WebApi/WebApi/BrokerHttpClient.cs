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
    public class BrokerHttpClient : RawHttpClientBase
    {
        public BrokerHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials)
            : base(baseUrl, credentials)
        {
        }

        public BrokerHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BrokerHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BrokerHttpClient(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BrokerHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<TaskAgentMessage> GetRunnerMessageAsync(
            string runnerVersion,
            TaskAgentStatus? status,
            CancellationToken cancellationToken = default
        )
        {
            var requestUri = new Uri(Client.BaseAddress, "message");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

            if (status != null)
            {
                queryParams.Add("status", status.Value.ToString());
            }
            if (runnerVersion != null)
            {
                queryParams.Add("runnerVersion", runnerVersion);
            }

            var result = await SendAsync<TaskAgentMessage>(
                new HttpMethod("GET"),
                requestUri: requestUri,
                queryParameters: queryParams,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            if (result.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AccessDeniedException(result.Error);
            }

            throw new Exception($"Failed to get job message: {result.Error}");
        }
    }
}
