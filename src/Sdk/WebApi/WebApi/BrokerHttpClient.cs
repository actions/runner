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
            Guid? sessionId,
            string runnerVersion,
            TaskAgentStatus? status,
            string os = null,
            string architecture = null,
            bool? disableUpdate = null,
            CancellationToken cancellationToken = default
        )
        {
            var requestUri = new Uri(Client.BaseAddress, "message");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

            if (sessionId != null)
            {
                queryParams.Add("sessionId", sessionId.Value.ToString());
            }

            if (status != null)
            {
                queryParams.Add("status", status.Value.ToString());
            }
            if (runnerVersion != null)
            {
                queryParams.Add("runnerVersion", runnerVersion);
            }

            if (os != null)
            {
                queryParams.Add("os", os);
            }

            if (architecture != null)
            {
                queryParams.Add("architecture", architecture);
            }

            if (disableUpdate != null)
            {
                queryParams.Add("disableUpdate", disableUpdate.Value.ToString().ToLower());
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

         public async Task<TaskAgentSession> CreateSessionAsync(
            TaskAgentSession session,
            CancellationToken cancellationToken = default)
        {

            var requestUri = new Uri(Client.BaseAddress, "session");
            var requestContent = new ObjectContent<TaskAgentSession>(session, new VssJsonMediaTypeFormatter(true));

            var result = await SendAsync<TaskAgentSession>(
                new HttpMethod("POST"),
                requestUri: requestUri,
                content: requestContent,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

             if (result.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AccessDeniedException(result.Error);
            }

            throw new Exception($"Failed to create broker session: {result.Error}");
        }
    }
}
