/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 *
 * ---------------------------------------------------------
 * Generated file, DO NOT EDIT
 * ---------------------------------------------------------
 *
 * See following wiki page for instructions on how to regenerate:
 *   https://aka.ms/azure-devops-client-generation
 *
 * Configuration file:
 *   distributedtask\client\webapi\clientgeneratorconfigs\genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public abstract class TaskAgentHttpClientBase : VssHttpClientBase
    {
        public TaskAgentHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskAgentHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskAgentHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskAgentHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskAgentHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Adds an agent to a pool.  You probably don't want to call this endpoint directly. Instead, [configure an agent](https://docs.microsoft.com/azure/devops/pipelines/agents/agents) using the agent download package.
        /// </summary>
        /// <param name="poolId">The agent pool in which to add the agent</param>
        /// <param name="agent">Details about the agent being added</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgent> AddAgentAsync(
            int poolId,
            TaskAgent agent,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgent>(agent, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete an agent.  You probably don't want to call this endpoint directly. Instead, [use the agent configuration script](https://docs.microsoft.com/azure/devops/pipelines/agents/agents) to remove an agent from your organization.
        /// </summary>
        /// <param name="poolId">The pool ID to remove the agent from</param>
        /// <param name="agentId">The agent ID to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteAgentAsync(
            int poolId,
            int agentId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId, agentId = agentId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get information about an agent.
        /// </summary>
        /// <param name="poolId">The agent pool containing the agent</param>
        /// <param name="agentId">The agent ID to get information about</param>
        /// <param name="includeCapabilities">Whether to include the agent's capabilities in the response</param>
        /// <param name="includeAssignedRequest">Whether to include details about the agent's current work</param>
        /// <param name="includeLastCompletedRequest">Whether to include details about the agents' most recent completed work</param>
        /// <param name="propertyFilters">Filter which custom properties will be returned</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgent> GetAgentAsync(
            int poolId,
            int agentId,
            bool? includeCapabilities = null,
            bool? includeAssignedRequest = null,
            bool? includeLastCompletedRequest = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId, agentId = agentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeCapabilities != null)
            {
                queryParams.Add("includeCapabilities", includeCapabilities.Value.ToString());
            }
            if (includeAssignedRequest != null)
            {
                queryParams.Add("includeAssignedRequest", includeAssignedRequest.Value.ToString());
            }
            if (includeLastCompletedRequest != null)
            {
                queryParams.Add("includeLastCompletedRequest", includeLastCompletedRequest.Value.ToString());
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agents.
        /// </summary>
        /// <param name="poolId">The agent pool containing the agents</param>
        /// <param name="agentName">Filter on agent name</param>
        /// <param name="includeCapabilities">Whether to include the agents' capabilities in the response</param>
        /// <param name="includeAssignedRequest">Whether to include details about the agents' current work</param>
        /// <param name="includeLastCompletedRequest">Whether to include details about the agents' most recent completed work</param>
        /// <param name="propertyFilters">Filter which custom properties will be returned</param>
        /// <param name="demands">Filter by demands the agents can satisfy</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgent>> GetAgentsAsync(
            int poolId,
            string agentName = null,
            bool? includeCapabilities = null,
            bool? includeAssignedRequest = null,
            bool? includeLastCompletedRequest = null,
            IEnumerable<string> propertyFilters = null,
            IEnumerable<string> demands = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (agentName != null)
            {
                queryParams.Add("agentName", agentName);
            }
            if (includeCapabilities != null)
            {
                queryParams.Add("includeCapabilities", includeCapabilities.Value.ToString());
            }
            if (includeAssignedRequest != null)
            {
                queryParams.Add("includeAssignedRequest", includeAssignedRequest.Value.ToString());
            }
            if (includeLastCompletedRequest != null)
            {
                queryParams.Add("includeLastCompletedRequest", includeLastCompletedRequest.Value.ToString());
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }
            if (demands != null && demands.Any())
            {
                queryParams.Add("demands", string.Join(",", demands));
            }

            return SendAsync<List<TaskAgent>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Replace an agent.  You probably don't want to call this endpoint directly. Instead, [use the agent configuration script](https://docs.microsoft.com/azure/devops/pipelines/agents/agents) to remove and reconfigure an agent from your organization.
        /// </summary>
        /// <param name="poolId">The agent pool to use</param>
        /// <param name="agentId">The agent to replace</param>
        /// <param name="agent">Updated details about the replacing agent</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgent> ReplaceAgentAsync(
            int poolId,
            int agentId,
            TaskAgent agent,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId, agentId = agentId };
            HttpContent content = new ObjectContent<TaskAgent>(agent, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update agent details.
        /// </summary>
        /// <param name="poolId">The agent pool to use</param>
        /// <param name="agentId">The agent to update</param>
        /// <param name="agent">Updated details about the agent</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgent> UpdateAgentAsync(
            int poolId,
            int agentId,
            TaskAgent agent,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("e298ef32-5878-4cab-993c-043836571f42");
            object routeValues = new { poolId = poolId, agentId = agentId };
            HttpContent content = new ObjectContent<TaskAgent>(agent, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="requestId"></param>
        /// <param name="lockToken"></param>
        /// <param name="result"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteAgentRequestAsync(
            int poolId,
            long requestId,
            Guid lockToken,
            TaskResult? result = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId, requestId = requestId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("lockToken", lockToken.ToString());
            if (result != null)
            {
                queryParams.Add("result", result.Value.ToString());
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="requestId"></param>
        /// <param name="includeStatus"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentJobRequest> GetAgentRequestAsync(
            int poolId,
            long requestId,
            bool? includeStatus = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId, requestId = requestId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeStatus != null)
            {
                queryParams.Add("includeStatus", includeStatus.Value.ToString());
            }

            return SendAsync<TaskAgentJobRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="requestId"></param>
        /// <param name="lockToken"></param>
        /// <param name="request"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentJobRequest> UpdateAgentRequestAsync(
            int poolId,
            long requestId,
            Guid lockToken,
            TaskAgentJobRequest request,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId, requestId = requestId };
            HttpContent content = new ObjectContent<TaskAgentJobRequest>(request, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("lockToken", lockToken.ToString());

            return SendAsync<TaskAgentJobRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="messageId"></param>
        /// <param name="sessionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteMessageAsync(
            int poolId,
            long messageId,
            Guid sessionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("c3a054f6-7a8a-49c0-944e-3a8e5d7adfd7");
            object routeValues = new { poolId = poolId, messageId = messageId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("sessionId", sessionId.ToString());

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="sessionId"></param>
        /// <param name="lastMessageId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentMessage> GetMessageAsync(
            int poolId,
            Guid sessionId,
            long? lastMessageId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("c3a054f6-7a8a-49c0-944e-3a8e5d7adfd7");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("sessionId", sessionId.ToString());
            if (lastMessageId != null)
            {
                queryParams.Add("lastMessageId", lastMessageId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<TaskAgentMessage>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="agentId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshAgentAsync(
            int poolId,
            int agentId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("c3a054f6-7a8a-49c0-944e-3a8e5d7adfd7");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("agentId", agentId.ToString(CultureInfo.InvariantCulture));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshAgentsAsync(
            int poolId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("c3a054f6-7a8a-49c0-944e-3a8e5d7adfd7");
            object routeValues = new { poolId = poolId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="requestId"></param>
        /// <param name="message"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task SendMessageAsync(
            int poolId,
            long requestId,
            TaskAgentMessage message,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("c3a054f6-7a8a-49c0-944e-3a8e5d7adfd7");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentMessage>(message, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("requestId", requestId.ToString(CultureInfo.InvariantCulture));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="packageType"></param>
        /// <param name="platform"></param>
        /// <param name="version"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<PackageMetadata> GetPackageAsync(
            string packageType,
            string platform,
            string version,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8ffcd551-079c-493a-9c02-54346299d144");
            object routeValues = new { packageType = packageType, platform = platform, version = version };

            return SendAsync<PackageMetadata>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="packageType"></param>
        /// <param name="platform"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<PackageMetadata>> GetPackagesAsync(
            string packageType,
            string platform = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8ffcd551-079c-493a-9c02-54346299d144");
            object routeValues = new { packageType = packageType, platform = platform };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<PackageMetadata>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent pools.
        /// </summary>
        /// <param name="poolName">Filter by name</param>
        /// <param name="properties">Filter by agent pool properties (comma-separated)</param>
        /// <param name="poolType">Filter by pool type</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentPool>> GetAgentPoolsAsync(
            string poolName = null,
            IEnumerable<string> properties = null,
            TaskAgentPoolType? poolType = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (poolName != null)
            {
                queryParams.Add("poolName", poolName);
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (poolType != null)
            {
                queryParams.Add("poolType", poolType.Value.ToString());
            }

            return SendAsync<List<TaskAgentPool>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="session"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentSession> CreateAgentSessionAsync(
            int poolId,
            TaskAgentSession session,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("134e239e-2df3-4794-a6f6-24f1f19ec8dc");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentSession>(session, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentSession>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="sessionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteAgentSessionAsync(
            int poolId,
            Guid sessionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("134e239e-2df3-4794-a6f6-24f1f19ec8dc");
            object routeValues = new { poolId = poolId, sessionId = sessionId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="agentId"></param>
        /// <param name="currentState"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgent> UpdateAgentUpdateStateAsync(
            int poolId,
            int agentId,
            string currentState,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("8cc1b02b-ae49-4516-b5ad-4f9b29967c30");
            object routeValues = new { poolId = poolId, agentId = agentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("currentState", currentState);

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="agentId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<String> GetAgentAuthUrlAsync(
            int poolId,
            int agentId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a82a119c-1e46-44b6-8d75-c82a79cf975b");
            object routeValues = new { poolId = poolId, agentId = agentId };

            return SendAsync<String>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="agentId"></param>
        /// <param name="error"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task ReportAgentAuthUrlMigrationErrorAsync(
            int poolId,
            int agentId,
            string error,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("a82a119c-1e46-44b6-8d75-c82a79cf975b");
            object routeValues = new { poolId = poolId, agentId = agentId };
            HttpContent content = new ObjectContent<string>(error, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }
    }
}
