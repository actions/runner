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
    public abstract class TaskAgentHttpClientBase : TaskAgentHttpClientCompatBase
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
        /// [Preview API]
        /// </summary>
        /// <param name="agentCloud"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentCloud> AddAgentCloudAsync(
            TaskAgentCloud agentCloud,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("bfa72b3d-0fc6-43fb-932b-a7f6559f93b9");
            HttpContent content = new ObjectContent<TaskAgentCloud>(agentCloud, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentCloud>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="agentCloudId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentCloud> DeleteAgentCloudAsync(
            int agentCloudId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("bfa72b3d-0fc6-43fb-932b-a7f6559f93b9");
            object routeValues = new { agentCloudId = agentCloudId };

            return SendAsync<TaskAgentCloud>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="agentCloudId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentCloud> GetAgentCloudAsync(
            int agentCloudId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("bfa72b3d-0fc6-43fb-932b-a7f6559f93b9");
            object routeValues = new { agentCloudId = agentCloudId };

            return SendAsync<TaskAgentCloud>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentCloud>> GetAgentCloudsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("bfa72b3d-0fc6-43fb-932b-a7f6559f93b9");

            return SendAsync<List<TaskAgentCloud>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get agent cloud types.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentCloudType>> GetAgentCloudTypesAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("5932e193-f376-469d-9c3e-e5588ce12cb5");

            return SendAsync<List<TaskAgentCloudType>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="queueId"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForQueueAsync(
            int queueId,
            int top,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5f81ffb-f396-498d-85b1-5ada145e648a");
            object routeValues = new { queueId = queueId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("$top", top.ToString(CultureInfo.InvariantCulture));
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="queueId"></param>
        /// <param name="request"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentJobRequest> QueueAgentRequestAsync(
            int queueId,
            TaskAgentJobRequest request,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f5f81ffb-f396-498d-85b1-5ada145e648a");
            object routeValues = new { queueId = queueId };
            HttpContent content = new ObjectContent<TaskAgentJobRequest>(request, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentJobRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
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
                version: new ApiResourceVersion(5.1, 1),
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
                version: new ApiResourceVersion(5.1, 1),
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
                version: new ApiResourceVersion(5.1, 1),
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
                version: new ApiResourceVersion(5.1, 1),
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
                version: new ApiResourceVersion(5.1, 1),
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
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Returns list of azure subscriptions
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<AzureManagementGroupQueryResult> GetAzureManagementGroupsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("39fe3bf2-7ee0-4198-a469-4a29929afa9c");

            return SendAsync<AzureManagementGroupQueryResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Returns list of azure subscriptions
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<AzureSubscriptionQueryResult> GetAzureSubscriptionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("bcd6189c-0303-471f-a8e1-acb22b74d700");

            return SendAsync<AzureSubscriptionQueryResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] GET a PAT token for managing (configuring, removing, tagging) deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment targets are managed.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GenerateDeploymentGroupAccessTokenAsync(
            string project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("3d197ba2-c3e9-4253-882f-0ee2440f8174");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] GET a PAT token for managing (configuring, removing, tagging) deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment targets are managed.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GenerateDeploymentGroupAccessTokenAsync(
            Guid project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("3d197ba2-c3e9-4253-882f-0ee2440f8174");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Create a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroup">Deployment group to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> AddDeploymentGroupAsync(
            string project,
            DeploymentGroupCreateParameter deploymentGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<DeploymentGroupCreateParameter>(deploymentGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroup">Deployment group to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> AddDeploymentGroupAsync(
            Guid project,
            DeploymentGroupCreateParameter deploymentGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<DeploymentGroupCreateParameter>(deploymentGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group to be deleted.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDeploymentGroupAsync(
            string project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// [Preview API] Delete a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group to be deleted.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDeploymentGroupAsync(
            Guid project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// [Preview API] Get a deployment group by its ID.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="actionFilter">Get the deployment group only if this action can be performed on it.</param>
        /// <param name="expand">Include these additional details in the returned object.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> GetDeploymentGroupAsync(
            string project,
            int deploymentGroupId,
            DeploymentGroupActionFilter? actionFilter = null,
            DeploymentGroupExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a deployment group by its ID.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="actionFilter">Get the deployment group only if this action can be performed on it.</param>
        /// <param name="expand">Include these additional details in the returned object.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> GetDeploymentGroupAsync(
            Guid project,
            int deploymentGroupId,
            DeploymentGroupActionFilter? actionFilter = null,
            DeploymentGroupExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment groups by name or IDs.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name">Name of the deployment group.</param>
        /// <param name="actionFilter">Get only deployment groups on which this action can be performed.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="continuationToken">Get deployment groups with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment groups to return. Default is **1000**.</param>
        /// <param name="ids">Comma separated list of IDs of the deployment groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentGroup>> GetDeploymentGroupsAsync(
            string project,
            string name = null,
            DeploymentGroupActionFilter? actionFilter = null,
            DeploymentGroupExpands? expand = null,
            string continuationToken = null,
            int? top = null,
            IEnumerable<int> ids = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (ids != null && ids.Any())
            {
                queryParams.Add("ids", string.Join(",", ids));
            }

            return SendAsync<List<DeploymentGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment groups by name or IDs.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name">Name of the deployment group.</param>
        /// <param name="actionFilter">Get only deployment groups on which this action can be performed.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="continuationToken">Get deployment groups with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment groups to return. Default is **1000**.</param>
        /// <param name="ids">Comma separated list of IDs of the deployment groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentGroup>> GetDeploymentGroupsAsync(
            Guid project,
            string name = null,
            DeploymentGroupActionFilter? actionFilter = null,
            DeploymentGroupExpands? expand = null,
            string continuationToken = null,
            int? top = null,
            IEnumerable<int> ids = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (ids != null && ids.Any())
            {
                queryParams.Add("ids", string.Join(",", ids));
            }

            return SendAsync<List<DeploymentGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Update a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="deploymentGroup">Deployment group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> UpdateDeploymentGroupAsync(
            string project,
            int deploymentGroupId,
            DeploymentGroupUpdateParameter deploymentGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentGroupUpdateParameter>(deploymentGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="deploymentGroup">Deployment group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentGroup> UpdateDeploymentGroupAsync(
            Guid project,
            int deploymentGroupId,
            DeploymentGroupUpdateParameter deploymentGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("083c4d89-ab35-45af-aa11-7cf66895c53e");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentGroupUpdateParameter>(deploymentGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment group metrics.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupName">Name of the deployment group.</param>
        /// <param name="continuationToken">Get metrics for deployment groups with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment group metrics to return. Default is **50**.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentGroupMetrics>> GetDeploymentGroupsMetricsAsync(
            string project,
            string deploymentGroupName = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("281c6308-427a-49e1-b83a-dac0f4862189");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (deploymentGroupName != null)
            {
                queryParams.Add("deploymentGroupName", deploymentGroupName);
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<DeploymentGroupMetrics>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment group metrics.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupName">Name of the deployment group.</param>
        /// <param name="continuationToken">Get metrics for deployment groups with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment group metrics to return. Default is **50**.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentGroupMetrics>> GetDeploymentGroupsMetricsAsync(
            Guid project,
            string deploymentGroupName = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("281c6308-427a-49e1-b83a-dac0f4862189");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (deploymentGroupName != null)
            {
                queryParams.Add("deploymentGroupName", deploymentGroupName);
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<DeploymentGroupMetrics>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            int machineId,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a3540e5b-f0dc-4668-963b-b752459be545");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("machineId", machineId.ToString(CultureInfo.InvariantCulture));
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            int machineId,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a3540e5b-f0dc-4668-963b-b752459be545");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("machineId", machineId.ToString(CultureInfo.InvariantCulture));
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineIds"></param>
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentMachinesAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<int> machineIds = null,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a3540e5b-f0dc-4668-963b-b752459be545");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (machineIds != null && machineIds.Any())
            {
                queryParams.Add("machineIds", string.Join(",", machineIds));
            }
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineIds"></param>
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentMachinesAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<int> machineIds = null,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a3540e5b-f0dc-4668-963b-b752459be545");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (machineIds != null && machineIds.Any())
            {
                queryParams.Add("machineIds", string.Join(",", machineIds));
            }
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshDeploymentMachinesAsync(
            string project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("91006ac4-0f68-4d82-a2bc-540676bd73ce");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshDeploymentMachinesAsync(
            Guid project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("91006ac4-0f68-4d82-a2bc-540676bd73ce");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// [Preview API] GET a PAT token for managing (configuring, removing, tagging) deployment agents in a deployment pool.
        /// </summary>
        /// <param name="poolId">ID of the deployment pool in which deployment agents are managed.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GenerateDeploymentPoolAccessTokenAsync(
            int poolId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("e077ee4a-399b-420b-841f-c43fbc058e0b");
            object routeValues = new { poolId = poolId };

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment pool summaries.
        /// </summary>
        /// <param name="poolName">Name of the deployment pool.</param>
        /// <param name="expands">Include these additional details in the returned objects.</param>
        /// <param name="poolIds">List of deployment pool ids.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentPoolSummary>> GetDeploymentPoolsSummaryAsync(
            string poolName = null,
            DeploymentPoolSummaryExpands? expands = null,
            IEnumerable<int> poolIds = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6525d6c6-258f-40e0-a1a9-8a24a3957625");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (poolName != null)
            {
                queryParams.Add("poolName", poolName);
            }
            if (expands != null)
            {
                queryParams.Add("expands", expands.Value.ToString());
            }
            if (poolIds != null && poolIds.Any())
            {
                queryParams.Add("poolIds", string.Join(",", poolIds));
            }

            return SendAsync<List<DeploymentPoolSummary>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get agent requests for a deployment target.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the target belongs.</param>
        /// <param name="targetId">ID of the deployment target.</param>
        /// <param name="completedRequestCount">Maximum number of completed requests to return. Default is **50**</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            int targetId,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2fac0be3-8c8f-4473-ab93-c1389b08a2c9");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("targetId", targetId.ToString(CultureInfo.InvariantCulture));
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get agent requests for a deployment target.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the target belongs.</param>
        /// <param name="targetId">ID of the deployment target.</param>
        /// <param name="completedRequestCount">Maximum number of completed requests to return. Default is **50**</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            int targetId,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2fac0be3-8c8f-4473-ab93-c1389b08a2c9");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("targetId", targetId.ToString(CultureInfo.InvariantCulture));
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get agent requests for a list deployment targets.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the targets belong.</param>
        /// <param name="targetIds">Comma separated list of IDs of the deployment targets.</param>
        /// <param name="ownerId">Id of owner of agent job request.</param>
        /// <param name="completedOn">Datetime to return request after this time.</param>
        /// <param name="completedRequestCount">Maximum number of completed requests to return for each target. Default is **50**</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentTargetsAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<int> targetIds = null,
            int? ownerId = null,
            DateTime? completedOn = null,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2fac0be3-8c8f-4473-ab93-c1389b08a2c9");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (targetIds != null && targetIds.Any())
            {
                queryParams.Add("targetIds", string.Join(",", targetIds));
            }
            if (ownerId != null)
            {
                queryParams.Add("ownerId", ownerId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (completedOn != null)
            {
                AddDateTimeToQueryParams(queryParams, "completedOn", completedOn.Value);
            }
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get agent requests for a list deployment targets.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the targets belong.</param>
        /// <param name="targetIds">Comma separated list of IDs of the deployment targets.</param>
        /// <param name="ownerId">Id of owner of agent job request.</param>
        /// <param name="completedOn">Datetime to return request after this time.</param>
        /// <param name="completedRequestCount">Maximum number of completed requests to return for each target. Default is **50**</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForDeploymentTargetsAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<int> targetIds = null,
            int? ownerId = null,
            DateTime? completedOn = null,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2fac0be3-8c8f-4473-ab93-c1389b08a2c9");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (targetIds != null && targetIds.Any())
            {
                queryParams.Add("targetIds", string.Join(",", targetIds));
            }
            if (ownerId != null)
            {
                queryParams.Add("ownerId", ownerId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (completedOn != null)
            {
                AddDateTimeToQueryParams(queryParams, "completedOn", completedOn.Value);
            }
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Upgrade the deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshDeploymentTargetsAsync(
            string project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("1c1a817f-f23d-41c6-bf8d-14b638f64152");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// [Preview API] Upgrade the deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task RefreshDeploymentTargetsAsync(
            Guid project,
            int deploymentGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("1c1a817f-f23d-41c6-bf8d-14b638f64152");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

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
        /// [Preview API] Proxy for a GET request defined by an 'endpoint'. The request is authorized using a service connection. The response is filtered using an XPath/Json based selector.
        /// </summary>
        /// <param name="endpoint">Describes the URL to fetch.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<string>> QueryEndpointAsync(
            TaskDefinitionEndpoint endpoint,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f223b809-8c33-4b7d-b53f-07232569b5d6");
            HttpContent content = new ObjectContent<TaskDefinitionEndpoint>(endpoint, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Get environment deployment execution history
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<EnvironmentDeploymentExecutionRecord>> GetEnvironmentDeploymentExecutionRecordsAsync(
            string project,
            int environmentId,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("51bb5d21-4305-4ea6-9dbb-b7488af73334");
            object routeValues = new { project = project, environmentId = environmentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<EnvironmentDeploymentExecutionRecord>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get environment deployment execution history
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<EnvironmentDeploymentExecutionRecord>> GetEnvironmentDeploymentExecutionRecordsAsync(
            Guid project,
            int environmentId,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("51bb5d21-4305-4ea6-9dbb-b7488af73334");
            object routeValues = new { project = project, environmentId = environmentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<EnvironmentDeploymentExecutionRecord>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Create an environment.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentCreateParameter">Environment to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> AddEnvironmentAsync(
            string project,
            EnvironmentCreateParameter environmentCreateParameter,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<EnvironmentCreateParameter>(environmentCreateParameter, new VssJsonMediaTypeFormatter(true));

            return SendAsync<EnvironmentInstance>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create an environment.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentCreateParameter">Environment to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> AddEnvironmentAsync(
            Guid project,
            EnvironmentCreateParameter environmentCreateParameter,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<EnvironmentCreateParameter>(environmentCreateParameter, new VssJsonMediaTypeFormatter(true));

            return SendAsync<EnvironmentInstance>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete the specified environment.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteEnvironmentAsync(
            string project,
            int environmentId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };

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
        /// [Preview API] Delete the specified environment.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteEnvironmentAsync(
            Guid project,
            int environmentId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };

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
        /// [Preview API] Get an environment by its ID.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="expands">Include these additional details in the returned objects.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> GetEnvironmentByIdAsync(
            string project,
            int environmentId,
            EnvironmentExpands? expands = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expands != null)
            {
                queryParams.Add("expands", expands.Value.ToString());
            }

            return SendAsync<EnvironmentInstance>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get an environment by its ID.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="expands">Include these additional details in the returned objects.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> GetEnvironmentByIdAsync(
            Guid project,
            int environmentId,
            EnvironmentExpands? expands = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expands != null)
            {
                queryParams.Add("expands", expands.Value.ToString());
            }

            return SendAsync<EnvironmentInstance>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get all environments.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<EnvironmentInstance>> GetEnvironmentsAsync(
            string project,
            string name = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<EnvironmentInstance>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get all environments.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<EnvironmentInstance>> GetEnvironmentsAsync(
            Guid project,
            string name = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<EnvironmentInstance>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Update the specified environment.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="environmentUpdateParameter">Environment data to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> UpdateEnvironmentAsync(
            string project,
            int environmentId,
            EnvironmentUpdateParameter environmentUpdateParameter,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<EnvironmentUpdateParameter>(environmentUpdateParameter, new VssJsonMediaTypeFormatter(true));

            return SendAsync<EnvironmentInstance>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update the specified environment.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId">ID of the environment.</param>
        /// <param name="environmentUpdateParameter">Environment data to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<EnvironmentInstance> UpdateEnvironmentAsync(
            Guid project,
            int environmentId,
            EnvironmentUpdateParameter environmentUpdateParameter,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("8572b1fc-2482-47fa-8f74-7e3ed53ee54b");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<EnvironmentUpdateParameter>(environmentUpdateParameter, new VssJsonMediaTypeFormatter(true));

            return SendAsync<EnvironmentInstance>(
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
        /// <param name="hubName"></param>
        /// <param name="includeEnterpriseUsersCount"></param>
        /// <param name="includeHostedAgentMinutesCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskHubLicenseDetails> GetTaskHubLicenseDetailsAsync(
            string hubName,
            bool? includeEnterpriseUsersCount = null,
            bool? includeHostedAgentMinutesCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f9f0f436-b8a1-4475-9041-1ccdbf8f0128");
            object routeValues = new { hubName = hubName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeEnterpriseUsersCount != null)
            {
                queryParams.Add("includeEnterpriseUsersCount", includeEnterpriseUsersCount.Value.ToString());
            }
            if (includeHostedAgentMinutesCount != null)
            {
                queryParams.Add("includeHostedAgentMinutesCount", includeHostedAgentMinutesCount.Value.ToString());
            }

            return SendAsync<TaskHubLicenseDetails>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 3),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="hubName"></param>
        /// <param name="taskHubLicenseDetails"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskHubLicenseDetails> UpdateTaskHubLicenseDetailsAsync(
            string hubName,
            TaskHubLicenseDetails taskHubLicenseDetails,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("f9f0f436-b8a1-4475-9041-1ccdbf8f0128");
            object routeValues = new { hubName = hubName };
            HttpContent content = new ObjectContent<TaskHubLicenseDetails>(taskHubLicenseDetails, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskHubLicenseDetails>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 3),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="versionString"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<StreamContent> GetTaskIconAsync(
            Guid taskId,
            string versionString,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("63463108-174d-49d4-b8cb-235eea42a5e1");
            object routeValues = new { taskId = taskId, versionString = versionString };

            return SendAsync<StreamContent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="inputValidationRequest"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<InputValidationRequest> ValidateInputsAsync(
            InputValidationRequest inputValidationRequest,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("58475b1e-adaf-4155-9bc1-e04bf1fff4c2");
            HttpContent content = new ObjectContent<InputValidationRequest>(inputValidationRequest, new VssJsonMediaTypeFormatter(true));

            return SendAsync<InputValidationRequest>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
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
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsAsync(
            int poolId,
            int top,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("$top", top.ToString(CultureInfo.InvariantCulture));
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForAgentAsync(
            int poolId,
            int agentId,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("agentId", agentId.ToString(CultureInfo.InvariantCulture));
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="agentIds"></param>
        /// <param name="completedRequestCount"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForAgentsAsync(
            int poolId,
            IEnumerable<int> agentIds = null,
            int? completedRequestCount = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (agentIds != null && agentIds.Any())
            {
                queryParams.Add("agentIds", string.Join(",", agentIds));
            }
            if (completedRequestCount != null)
            {
                queryParams.Add("completedRequestCount", completedRequestCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="planId"></param>
        /// <param name="jobId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentJobRequest>> GetAgentRequestsForPlanAsync(
            int poolId,
            Guid planId,
            Guid? jobId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("planId", planId.ToString());
            if (jobId != null)
            {
                queryParams.Add("jobId", jobId.Value.ToString());
            }

            return SendAsync<List<TaskAgentJobRequest>>(
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
        /// <param name="request"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentJobRequest> QueueAgentRequestByPoolAsync(
            int poolId,
            TaskAgentJobRequest request,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("fc825784-c92a-4299-9221-998a02d1b54f");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentJobRequest>(request, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentJobRequest>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="createParameters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<KubernetesResource> AddKubernetesResourceAsync(
            string project,
            int environmentId,
            KubernetesResourceCreateParameters createParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<KubernetesResourceCreateParameters>(createParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<KubernetesResource>(
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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="createParameters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<KubernetesResource> AddKubernetesResourceAsync(
            Guid project,
            int environmentId,
            KubernetesResourceCreateParameters createParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<KubernetesResourceCreateParameters>(createParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<KubernetesResource>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteKubernetesResourceAsync(
            string project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteKubernetesResourceAsync(
            Guid project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<KubernetesResource> GetKubernetesResourceAsync(
            string project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            return SendAsync<KubernetesResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<KubernetesResource> GetKubernetesResourceAsync(
            Guid project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("73fba52f-15ab-42b3-a538-ce67a9223a04");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            return SendAsync<KubernetesResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GenerateDeploymentMachineGroupAccessTokenAsync(
            string project,
            int machineGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f8c7c0de-ac0d-469b-9cb1-c21f72d67693");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GenerateDeploymentMachineGroupAccessTokenAsync(
            Guid project,
            int machineGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f8c7c0de-ac0d-469b-9cb1-c21f72d67693");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> AddDeploymentMachineGroupAsync(
            string project,
            DeploymentMachineGroup machineGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<DeploymentMachineGroup>(machineGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> AddDeploymentMachineGroupAsync(
            Guid project,
            DeploymentMachineGroup machineGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<DeploymentMachineGroup>(machineGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteDeploymentMachineGroupAsync(
            string project,
            int machineGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteDeploymentMachineGroupAsync(
            Guid project,
            int machineGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> GetDeploymentMachineGroupAsync(
            string project,
            int machineGroupId,
            MachineGroupActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> GetDeploymentMachineGroupAsync(
            Guid project,
            int machineGroupId,
            MachineGroupActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupName"></param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachineGroup>> GetDeploymentMachineGroupsAsync(
            string project,
            string machineGroupName = null,
            MachineGroupActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (machineGroupName != null)
            {
                queryParams.Add("machineGroupName", machineGroupName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<DeploymentMachineGroup>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupName"></param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachineGroup>> GetDeploymentMachineGroupsAsync(
            Guid project,
            string machineGroupName = null,
            MachineGroupActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (machineGroupName != null)
            {
                queryParams.Add("machineGroupName", machineGroupName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<DeploymentMachineGroup>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="machineGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> UpdateDeploymentMachineGroupAsync(
            string project,
            int machineGroupId,
            DeploymentMachineGroup machineGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };
            HttpContent content = new ObjectContent<DeploymentMachineGroup>(machineGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="machineGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachineGroup> UpdateDeploymentMachineGroupAsync(
            Guid project,
            int machineGroupId,
            DeploymentMachineGroup machineGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("d4adf50f-80c6-4ac8-9ca1-6e4e544286e9");
            object routeValues = new { project = project, machineGroupId = machineGroupId };
            HttpContent content = new ObjectContent<DeploymentMachineGroup>(machineGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachineGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="tagFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> GetDeploymentMachineGroupMachinesAsync(
            string project,
            int machineGroupId,
            IEnumerable<string> tagFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("966c3874-c347-4b18-a90c-d509116717fd");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="tagFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> GetDeploymentMachineGroupMachinesAsync(
            Guid project,
            int machineGroupId,
            IEnumerable<string> tagFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("966c3874-c347-4b18-a90c-d509116717fd");
            object routeValues = new { project = project, machineGroupId = machineGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="machineGroupId"></param>
        /// <param name="deploymentMachines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentMachineGroupMachinesAsync(
            string project,
            int machineGroupId,
            IEnumerable<DeploymentMachine> deploymentMachines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("966c3874-c347-4b18-a90c-d509116717fd");
            object routeValues = new { project = project, machineGroupId = machineGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentMachine>>(deploymentMachines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="machineGroupId"></param>
        /// <param name="deploymentMachines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentMachineGroupMachinesAsync(
            Guid project,
            int machineGroupId,
            IEnumerable<DeploymentMachine> deploymentMachines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("966c3874-c347-4b18-a90c-d509116717fd");
            object routeValues = new { project = project, machineGroupId = machineGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentMachine>>(deploymentMachines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> AddDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> AddDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            int machineId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };

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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            int machineId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };

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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> GetDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachineExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> GetDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachineExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="tags"></param>
        /// <param name="name"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<string> tags = null,
            string name = null,
            DeploymentMachineExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="tags"></param>
        /// <param name="name"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<string> tags = null,
            string name = null,
            DeploymentMachineExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> ReplaceDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> ReplaceDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> UpdateDeploymentMachineAsync(
            string project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machineId"></param>
        /// <param name="machine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> UpdateDeploymentMachineAsync(
            Guid project,
            int deploymentGroupId,
            int machineId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, machineId = machineId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentMachinesAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<DeploymentMachine> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentMachine>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="machines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentMachinesAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<DeploymentMachine> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6f6d406f-cfe6-409c-9327-7009928077e7");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentMachine>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
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
        /// <param name="definition"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceDefinition> CreateAgentPoolMaintenanceDefinitionAsync(
            int poolId,
            TaskAgentPoolMaintenanceDefinition definition,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("80572e16-58f0-4419-ac07-d19fde32195c");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentPoolMaintenanceDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPoolMaintenanceDefinition>(
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
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteAgentPoolMaintenanceDefinitionAsync(
            int poolId,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("80572e16-58f0-4419-ac07-d19fde32195c");
            object routeValues = new { poolId = poolId, definitionId = definitionId };

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
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceDefinition> GetAgentPoolMaintenanceDefinitionAsync(
            int poolId,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("80572e16-58f0-4419-ac07-d19fde32195c");
            object routeValues = new { poolId = poolId, definitionId = definitionId };

            return SendAsync<TaskAgentPoolMaintenanceDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentPoolMaintenanceDefinition>> GetAgentPoolMaintenanceDefinitionsAsync(
            int poolId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("80572e16-58f0-4419-ac07-d19fde32195c");
            object routeValues = new { poolId = poolId };

            return SendAsync<List<TaskAgentPoolMaintenanceDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="definitionId"></param>
        /// <param name="definition"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceDefinition> UpdateAgentPoolMaintenanceDefinitionAsync(
            int poolId,
            int definitionId,
            TaskAgentPoolMaintenanceDefinition definition,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("80572e16-58f0-4419-ac07-d19fde32195c");
            object routeValues = new { poolId = poolId, definitionId = definitionId };
            HttpContent content = new ObjectContent<TaskAgentPoolMaintenanceDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPoolMaintenanceDefinition>(
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
        /// <param name="jobId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteAgentPoolMaintenanceJobAsync(
            int poolId,
            int jobId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId, jobId = jobId };

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
        /// <param name="jobId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceJob> GetAgentPoolMaintenanceJobAsync(
            int poolId,
            int jobId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId, jobId = jobId };

            return SendAsync<TaskAgentPoolMaintenanceJob>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="jobId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> GetAgentPoolMaintenanceJobLogsAsync(
            int poolId,
            int jobId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId, jobId = jobId };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                mediaType: "application/zip",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskAgentPoolMaintenanceJob>> GetAgentPoolMaintenanceJobsAsync(
            int poolId,
            int? definitionId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitionId != null)
            {
                queryParams.Add("definitionId", definitionId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskAgentPoolMaintenanceJob>>(
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
        /// <param name="job"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceJob> QueueAgentPoolMaintenanceJobAsync(
            int poolId,
            TaskAgentPoolMaintenanceJob job,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentPoolMaintenanceJob>(job, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPoolMaintenanceJob>(
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
        /// <param name="jobId"></param>
        /// <param name="job"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentPoolMaintenanceJob> UpdateAgentPoolMaintenanceJobAsync(
            int poolId,
            int jobId,
            TaskAgentPoolMaintenanceJob job,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("15e7ab6e-abce-4601-a6d8-e111fe148f46");
            object routeValues = new { poolId = poolId, jobId = jobId };
            HttpContent content = new ObjectContent<TaskAgentPoolMaintenanceJob>(job, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPoolMaintenanceJob>(
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
        /// [Preview API]
        /// </summary>
        /// <param name="poolId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> GetAgentPoolMetadataAsync(
            int poolId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0d62f887-9f53-48b9-9161-4c35d5735b0f");
            object routeValues = new { poolId = poolId };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                mediaType: "text/plain",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API] Create an agent pool.
        /// </summary>
        /// <param name="pool">Details about the new agent pool</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentPool> AddAgentPoolAsync(
            TaskAgentPool pool,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");
            HttpContent content = new ObjectContent<TaskAgentPool>(pool, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPool>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete an agent pool.
        /// </summary>
        /// <param name="poolId">ID of the agent pool to delete</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteAgentPoolAsync(
            int poolId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");
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
        /// [Preview API] Get information about an agent pool.
        /// </summary>
        /// <param name="poolId">An agent pool ID</param>
        /// <param name="properties">Agent pool properties (comma-separated)</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentPool> GetAgentPoolAsync(
            int poolId,
            IEnumerable<string> properties = null,
            TaskAgentPoolActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");
            object routeValues = new { poolId = poolId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<TaskAgentPool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
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
            TaskAgentPoolActionFilter? actionFilter = null,
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
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
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
        /// [Preview API] Get a list of agent pools.
        /// </summary>
        /// <param name="poolIds">pool Ids to fetch</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentPool>> GetAgentPoolsByIdsAsync(
            IEnumerable<int> poolIds,
            TaskAgentPoolActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string poolIdsAsString = null;
            if (poolIds != null)
            {
                poolIdsAsString = string.Join(",", poolIds);
            }
            queryParams.Add("poolIds", poolIdsAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
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
        /// [Preview API] Update properties on an agent pool
        /// </summary>
        /// <param name="poolId">The agent pool to update</param>
        /// <param name="pool">Updated agent pool details</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentPool> UpdateAgentPoolAsync(
            int poolId,
            TaskAgentPool pool,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("a8c47e17-4d56-4a56-92bb-de7ea7dc65be");
            object routeValues = new { poolId = poolId };
            HttpContent content = new ObjectContent<TaskAgentPool>(pool, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgentPool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create a new agent queue to connect a project to an agent pool.
        /// </summary>
        /// <param name="queue">Details about the queue to create</param>
        /// <param name="authorizePipelines">Automatically authorize this queue when using YAML</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> AddAgentQueueAsync(
            TaskAgentQueue queue,
            bool? authorizePipelines = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            HttpContent content = new ObjectContent<TaskAgentQueue>(queue, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (authorizePipelines != null)
            {
                queryParams.Add("authorizePipelines", authorizePipelines.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create a new agent queue to connect a project to an agent pool.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queue">Details about the queue to create</param>
        /// <param name="authorizePipelines">Automatically authorize this queue when using YAML</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> AddAgentQueueAsync(
            string project,
            TaskAgentQueue queue,
            bool? authorizePipelines = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskAgentQueue>(queue, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (authorizePipelines != null)
            {
                queryParams.Add("authorizePipelines", authorizePipelines.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
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
        /// [Preview API] Create a new agent queue to connect a project to an agent pool.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queue">Details about the queue to create</param>
        /// <param name="authorizePipelines">Automatically authorize this queue when using YAML</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> AddAgentQueueAsync(
            Guid project,
            TaskAgentQueue queue,
            bool? authorizePipelines = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskAgentQueue>(queue, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (authorizePipelines != null)
            {
                queryParams.Add("authorizePipelines", authorizePipelines.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
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
        /// [Preview API] Create a new team project.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task CreateTeamProjectAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Create a new team project.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task CreateTeamProjectAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

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
        /// [Preview API] Create a new team project.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task CreateTeamProjectAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

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
        /// [Preview API] Removes an agent queue from a project.
        /// </summary>
        /// <param name="queueId">The agent queue to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteAgentQueueAsync(
            int queueId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { queueId = queueId };

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
        /// [Preview API] Removes an agent queue from a project.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queueId">The agent queue to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteAgentQueueAsync(
            string project,
            int queueId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project, queueId = queueId };

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
        /// [Preview API] Removes an agent queue from a project.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queueId">The agent queue to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteAgentQueueAsync(
            Guid project,
            int queueId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project, queueId = queueId };

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
        /// [Preview API] Get information about an agent queue.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queueId">The agent queue to get information about</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> GetAgentQueueAsync(
            string project,
            int queueId,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project, queueId = queueId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get information about an agent queue.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queueId">The agent queue to get information about</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> GetAgentQueueAsync(
            Guid project,
            int queueId,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project, queueId = queueId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get information about an agent queue.
        /// </summary>
        /// <param name="queueId">The agent queue to get information about</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAgentQueue> GetAgentQueueAsync(
            int queueId,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { queueId = queueId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<TaskAgentQueue>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queueName">Filter on the agent queue name</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesAsync(
            string project,
            string queueName = null,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (queueName != null)
            {
                queryParams.Add("queueName", queueName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queueName">Filter on the agent queue name</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesAsync(
            Guid project,
            string queueName = null,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (queueName != null)
            {
                queryParams.Add("queueName", queueName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues.
        /// </summary>
        /// <param name="queueName">Filter on the agent queue name</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesAsync(
            string queueName = null,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (queueName != null)
            {
                queryParams.Add("queueName", queueName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their IDs
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queueIds">A comma-separated list of agent queue IDs to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByIdsAsync(
            string project,
            IEnumerable<int> queueIds,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueIdsAsString = null;
            if (queueIds != null)
            {
                queueIdsAsString = string.Join(",", queueIds);
            }
            queryParams.Add("queueIds", queueIdsAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their IDs
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queueIds">A comma-separated list of agent queue IDs to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByIdsAsync(
            Guid project,
            IEnumerable<int> queueIds,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueIdsAsString = null;
            if (queueIds != null)
            {
                queueIdsAsString = string.Join(",", queueIds);
            }
            queryParams.Add("queueIds", queueIdsAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their IDs
        /// </summary>
        /// <param name="queueIds">A comma-separated list of agent queue IDs to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByIdsAsync(
            IEnumerable<int> queueIds,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueIdsAsString = null;
            if (queueIds != null)
            {
                queueIdsAsString = string.Join(",", queueIds);
            }
            queryParams.Add("queueIds", queueIdsAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their names
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="queueNames">A comma-separated list of agent names to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByNamesAsync(
            string project,
            IEnumerable<string> queueNames,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueNamesAsString = null;
            if (queueNames != null)
            {
                queueNamesAsString = string.Join(",", queueNames);
            }
            queryParams.Add("queueNames", queueNamesAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their names
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="queueNames">A comma-separated list of agent names to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByNamesAsync(
            Guid project,
            IEnumerable<string> queueNames,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueNamesAsString = null;
            if (queueNames != null)
            {
                queueNamesAsString = string.Join(",", queueNames);
            }
            queryParams.Add("queueNames", queueNamesAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of agent queues by their names
        /// </summary>
        /// <param name="queueNames">A comma-separated list of agent names to retrieve</param>
        /// <param name="actionFilter">Filter by whether the calling user has use or manage permissions</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentQueue>> GetAgentQueuesByNamesAsync(
            IEnumerable<string> queueNames,
            TaskAgentQueueActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("900fa995-c559-4923-aae7-f8424fe4fbea");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string queueNamesAsString = null;
            if (queueNames != null)
            {
                queueNamesAsString = string.Join(",", queueNames);
            }
            queryParams.Add("queueNames", queueNamesAsString);
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<TaskAgentQueue>>(
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
        /// <param name="agentCloudId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAgentCloudRequest>> GetAgentCloudRequestsAsync(
            int agentCloudId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("20189bd7-5134-49c2-b8e9-f9e856eea2b2");
            object routeValues = new { agentCloudId = agentCloudId };

            return SendAsync<List<TaskAgentCloudRequest>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<ResourceLimit>> GetResourceLimitsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1f1f0557-c445-42a6-b4a0-0df605a3a0f8");

            return SendAsync<List<ResourceLimit>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="parallelismTag"></param>
        /// <param name="poolIsHosted"></param>
        /// <param name="includeRunningRequests"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<ResourceUsage> GetResourceUsageAsync(
            string parallelismTag = null,
            bool? poolIsHosted = null,
            bool? includeRunningRequests = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("eae1d376-a8b1-4475-9041-1dfdbe8f0143");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (parallelismTag != null)
            {
                queryParams.Add("parallelismTag", parallelismTag);
            }
            if (poolIsHosted != null)
            {
                queryParams.Add("poolIsHosted", poolIsHosted.Value.ToString());
            }
            if (includeRunningRequests != null)
            {
                queryParams.Add("includeRunningRequests", includeRunningRequests.Value.ToString());
            }

            return SendAsync<ResourceUsage>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroupRevision>> GetTaskGroupHistoryAsync(
            string project,
            Guid taskGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("100cc92a-b255-47fa-9ab3-e44a2985a3ac");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            return SendAsync<List<TaskGroupRevision>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroupRevision>> GetTaskGroupHistoryAsync(
            Guid project,
            Guid taskGroupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("100cc92a-b255-47fa-9ab3-e44a2985a3ac");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            return SendAsync<List<TaskGroupRevision>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Delete a secure file
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteSecureFileAsync(
            string project,
            Guid secureFileId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

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
        /// [Preview API] Delete a secure file
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteSecureFileAsync(
            Guid project,
            Guid secureFileId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

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
        /// [Preview API] Download a secure file by Id
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="ticket">A valid download ticket</param>
        /// <param name="download">If download is true, the file is sent as attachement in the response body. If download is false, the response body contains the file stream.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> DownloadSecureFileAsync(
            string project,
            Guid secureFileId,
            string ticket,
            bool? download = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("ticket", ticket);
            if (download != null)
            {
                queryParams.Add("download", download.Value.ToString());
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "application/octet-stream",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API] Download a secure file by Id
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="ticket">A valid download ticket</param>
        /// <param name="download">If download is true, the file is sent as attachement in the response body. If download is false, the response body contains the file stream.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> DownloadSecureFileAsync(
            Guid project,
            Guid secureFileId,
            string ticket,
            bool? download = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("ticket", ticket);
            if (download != null)
            {
                queryParams.Add("download", download.Value.ToString());
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "application/octet-stream",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API] Get a secure file
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="includeDownloadTicket">If includeDownloadTicket is true and the caller has permissions, a download ticket is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> GetSecureFileAsync(
            string project,
            Guid secureFileId,
            bool? includeDownloadTicket = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeDownloadTicket != null)
            {
                queryParams.Add("includeDownloadTicket", includeDownloadTicket.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<SecureFile>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a secure file
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="includeDownloadTicket">If includeDownloadTicket is true and the caller has permissions, a download ticket is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> GetSecureFileAsync(
            Guid project,
            Guid secureFileId,
            bool? includeDownloadTicket = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeDownloadTicket != null)
            {
                queryParams.Add("includeDownloadTicket", includeDownloadTicket.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<SecureFile>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter">Filter by secure file permissions for View, Manage or Use action. Defaults to View.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesAsync(
            string project,
            string namePattern = null,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (namePattern != null)
            {
                queryParams.Add("namePattern", namePattern);
            }
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter">Filter by secure file permissions for View, Manage or Use action. Defaults to View.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesAsync(
            Guid project,
            string namePattern = null,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (namePattern != null)
            {
                queryParams.Add("namePattern", namePattern);
            }
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileIds">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesByIdsAsync(
            string project,
            IEnumerable<Guid> secureFileIds,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string secureFileIdsAsString = null;
            if (secureFileIds != null)
            {
                secureFileIdsAsString = string.Join(",", secureFileIds);
            }
            queryParams.Add("secureFileIds", secureFileIdsAsString);
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileIds">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesByIdsAsync(
            Guid project,
            IEnumerable<Guid> secureFileIds,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string secureFileIdsAsString = null;
            if (secureFileIds != null)
            {
                secureFileIdsAsString = string.Join(",", secureFileIds);
            }
            queryParams.Add("secureFileIds", secureFileIdsAsString);
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileNames">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesByNamesAsync(
            string project,
            IEnumerable<string> secureFileNames,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string secureFileNamesAsString = null;
            if (secureFileNames != null)
            {
                secureFileNamesAsString = string.Join(",", secureFileNames);
            }
            queryParams.Add("secureFileNames", secureFileNamesAsString);
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileNames">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="actionFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> GetSecureFilesByNamesAsync(
            Guid project,
            IEnumerable<string> secureFileNames,
            bool? includeDownloadTickets = null,
            SecureFileActionFilter? actionFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string secureFileNamesAsString = null;
            if (secureFileNames != null)
            {
                secureFileNamesAsString = string.Join(",", secureFileNames);
            }
            queryParams.Add("secureFileNames", secureFileNamesAsString);
            if (includeDownloadTickets != null)
            {
                queryParams.Add("includeDownloadTickets", includeDownloadTickets.Value.ToString());
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Query secure files using a name pattern and a condition on file properties.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="condition">The main condition syntax is described [here](https://go.microsoft.com/fwlink/?linkid=842996). Use the *property('property-name')* function to access the value of the specified property of a secure file. It returns null if the property is not set. E.g. ``` and( eq( property('devices'), '2' ), in( property('provisioning profile type'), 'ad hoc', 'development' ) ) ```</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> QuerySecureFilesByPropertiesAsync(
            string project,
            string condition,
            string namePattern = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<string>(condition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (namePattern != null)
            {
                queryParams.Add("namePattern", namePattern);
            }

            return SendAsync<List<SecureFile>>(
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
        /// [Preview API] Query secure files using a name pattern and a condition on file properties.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="condition">The main condition syntax is described [here](https://go.microsoft.com/fwlink/?linkid=842996). Use the *property('property-name')* function to access the value of the specified property of a secure file. It returns null if the property is not set. E.g. ``` and( eq( property('devices'), '2' ), in( property('provisioning profile type'), 'ad hoc', 'development' ) ) ```</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> QuerySecureFilesByPropertiesAsync(
            Guid project,
            string condition,
            string namePattern = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<string>(condition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (namePattern != null)
            {
                queryParams.Add("namePattern", namePattern);
            }

            return SendAsync<List<SecureFile>>(
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
        /// [Preview API] Update the name or properties of an existing secure file
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="secureFile">The secure file with updated name and/or properties</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> UpdateSecureFileAsync(
            string project,
            Guid secureFileId,
            SecureFile secureFile,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };
            HttpContent content = new ObjectContent<SecureFile>(secureFile, new VssJsonMediaTypeFormatter(true));

            return SendAsync<SecureFile>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update the name or properties of an existing secure file
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="secureFile">The secure file with updated name and/or properties</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> UpdateSecureFileAsync(
            Guid project,
            Guid secureFileId,
            SecureFile secureFile,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project, secureFileId = secureFileId };
            HttpContent content = new ObjectContent<SecureFile>(secureFile, new VssJsonMediaTypeFormatter(true));

            return SendAsync<SecureFile>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update properties and/or names of a set of secure files. Files are identified by their IDs. Properties provided override the existing one entirely, i.e. do not merge.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFiles">A list of secure file objects. Only three field must be populated Id, Name, and Properties. The rest of fields in the object are ignored.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> UpdateSecureFilesAsync(
            string project,
            IEnumerable<SecureFile> secureFiles,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<SecureFile>>(secureFiles, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update properties and/or names of a set of secure files. Files are identified by their IDs. Properties provided override the existing one entirely, i.e. do not merge.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFiles">A list of secure file objects. Only three field must be populated Id, Name, and Properties. The rest of fields in the object are ignored.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<SecureFile>> UpdateSecureFilesAsync(
            Guid project,
            IEnumerable<SecureFile> secureFiles,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<SecureFile>>(secureFiles, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<SecureFile>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Upload a secure file, include the file stream in the request body
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="uploadStream">Stream to upload</param>
        /// <param name="name">Name of the file to upload</param>
        /// <param name="authorizePipelines">If authorizePipelines is true, then the secure file is authorized for use by all pipelines in the project.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> UploadSecureFileAsync(
            string project,
            Stream uploadStream,
            string name,
            bool? authorizePipelines = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new StreamContent(uploadStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("name", name);
            if (authorizePipelines != null)
            {
                queryParams.Add("authorizePipelines", authorizePipelines.Value.ToString());
            }

            return SendAsync<SecureFile>(
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
        /// [Preview API] Upload a secure file, include the file stream in the request body
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="uploadStream">Stream to upload</param>
        /// <param name="name">Name of the file to upload</param>
        /// <param name="authorizePipelines">If authorizePipelines is true, then the secure file is authorized for use by all pipelines in the project.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<SecureFile> UploadSecureFileAsync(
            Guid project,
            Stream uploadStream,
            string name,
            bool? authorizePipelines = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("adcfd8bc-b184-43ba-bd84-7c8c6a2ff421");
            object routeValues = new { project = project };
            HttpContent content = new StreamContent(uploadStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("name", name);
            if (authorizePipelines != null)
            {
                queryParams.Add("authorizePipelines", authorizePipelines.Value.ToString());
            }

            return SendAsync<SecureFile>(
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
        /// [Preview API] Register a deployment target to a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the deployment target is registered.</param>
        /// <param name="machine">Deployment target to register.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> AddDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Register a deployment target to a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which the deployment target is registered.</param>
        /// <param name="machine">Deployment target to register.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> AddDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete a deployment target in a deployment group. This deletes the agent from associated deployment pool too.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is deleted.</param>
        /// <param name="targetId">ID of the deployment target to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            int targetId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };

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
        /// [Preview API] Delete a deployment target in a deployment group. This deletes the agent from associated deployment pool too.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is deleted.</param>
        /// <param name="targetId">ID of the deployment target to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            int targetId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };

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
        /// [Preview API] Get a deployment target by its ID in a deployment group
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which deployment target belongs.</param>
        /// <param name="targetId">ID of the deployment target to return.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentMachine> GetDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            int targetId,
            DeploymentTargetExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a deployment target by its ID in a deployment group
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group to which deployment target belongs.</param>
        /// <param name="targetId">ID of the deployment target to return.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<DeploymentMachine> GetDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            int targetId,
            DeploymentTargetExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="tags">Get only the deployment targets that contain all these comma separted list of tags.</param>
        /// <param name="name">Name pattern of the deployment targets to return.</param>
        /// <param name="partialNameMatch">When set to true, treats **name** as pattern. Else treats it as absolute match. Default is **false**.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="agentStatus">Get only deployment targets that have this status.</param>
        /// <param name="agentJobResult">Get only deployment targets that have this last job result.</param>
        /// <param name="continuationToken">Get deployment targets with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment targets to return. Default is **1000**.</param>
        /// <param name="enabled">Get only deployment targets that are enabled or disabled. Default is 'null' which returns all the targets.</param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentMachine>> GetDeploymentTargetsAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<string> tags = null,
            string name = null,
            bool? partialNameMatch = null,
            DeploymentTargetExpands? expand = null,
            TaskAgentStatusFilter? agentStatus = null,
            TaskAgentJobResultFilter? agentJobResult = null,
            string continuationToken = null,
            int? top = null,
            bool? enabled = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (partialNameMatch != null)
            {
                queryParams.Add("partialNameMatch", partialNameMatch.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }
            if (agentStatus != null)
            {
                queryParams.Add("agentStatus", agentStatus.Value.ToString());
            }
            if (agentJobResult != null)
            {
                queryParams.Add("agentJobResult", agentJobResult.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (enabled != null)
            {
                queryParams.Add("enabled", enabled.Value.ToString());
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<List<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group.</param>
        /// <param name="tags">Get only the deployment targets that contain all these comma separted list of tags.</param>
        /// <param name="name">Name pattern of the deployment targets to return.</param>
        /// <param name="partialNameMatch">When set to true, treats **name** as pattern. Else treats it as absolute match. Default is **false**.</param>
        /// <param name="expand">Include these additional details in the returned objects.</param>
        /// <param name="agentStatus">Get only deployment targets that have this status.</param>
        /// <param name="agentJobResult">Get only deployment targets that have this last job result.</param>
        /// <param name="continuationToken">Get deployment targets with names greater than this continuationToken lexicographically.</param>
        /// <param name="top">Maximum number of deployment targets to return. Default is **1000**.</param>
        /// <param name="enabled">Get only deployment targets that are enabled or disabled. Default is 'null' which returns all the targets.</param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentMachine>> GetDeploymentTargetsAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<string> tags = null,
            string name = null,
            bool? partialNameMatch = null,
            DeploymentTargetExpands? expand = null,
            TaskAgentStatusFilter? agentStatus = null,
            TaskAgentJobResultFilter? agentJobResult = null,
            string continuationToken = null,
            int? top = null,
            bool? enabled = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (partialNameMatch != null)
            {
                queryParams.Add("partialNameMatch", partialNameMatch.Value.ToString());
            }
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }
            if (agentStatus != null)
            {
                queryParams.Add("agentStatus", agentStatus.Value.ToString());
            }
            if (agentJobResult != null)
            {
                queryParams.Add("agentJobResult", agentJobResult.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (enabled != null)
            {
                queryParams.Add("enabled", enabled.Value.ToString());
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<List<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Replace a deployment target in a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is replaced.</param>
        /// <param name="targetId">ID of the deployment target to replace.</param>
        /// <param name="machine">New deployment target.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> ReplaceDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            int targetId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Replace a deployment target in a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is replaced.</param>
        /// <param name="targetId">ID of the deployment target to replace.</param>
        /// <param name="machine">New deployment target.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> ReplaceDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            int targetId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a deployment target and its agent properties in a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is updated.</param>
        /// <param name="targetId">ID of the deployment target to update.</param>
        /// <param name="machine">Deployment target to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> UpdateDeploymentTargetAsync(
            string project,
            int deploymentGroupId,
            int targetId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a deployment target and its agent properties in a deployment group. Generally this is called by agent configuration tool.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment target is updated.</param>
        /// <param name="targetId">ID of the deployment target to update.</param>
        /// <param name="machine">Deployment target to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<DeploymentMachine> UpdateDeploymentTargetAsync(
            Guid project,
            int deploymentGroupId,
            int targetId,
            DeploymentMachine machine,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId, targetId = targetId };
            HttpContent content = new ObjectContent<DeploymentMachine>(machine, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DeploymentMachine>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update tags of a list of deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment targets are updated.</param>
        /// <param name="machines">Deployment targets with tags to udpdate.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentTargetsAsync(
            string project,
            int deploymentGroupId,
            IEnumerable<DeploymentTargetUpdateParameter> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentTargetUpdateParameter>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update tags of a list of deployment targets in a deployment group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId">ID of the deployment group in which deployment targets are updated.</param>
        /// <param name="machines">Deployment targets with tags to udpdate.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DeploymentMachine>> UpdateDeploymentTargetsAsync(
            Guid project,
            int deploymentGroupId,
            IEnumerable<DeploymentTargetUpdateParameter> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };
            HttpContent content = new ObjectContent<IEnumerable<DeploymentTargetUpdateParameter>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create a task group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroup">Task group object to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskGroup> AddTaskGroupAsync(
            string project,
            TaskGroupCreateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroupCreateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Create a task group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroup">Task group object to create.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskGroup> AddTaskGroupAsync(
            Guid project,
            TaskGroupCreateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroupCreateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete a task group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId">Id of the task group to be deleted.</param>
        /// <param name="comment">Comments to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTaskGroupAsync(
            string project,
            Guid taskGroupId,
            string comment = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (comment != null)
            {
                queryParams.Add("comment", comment);
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
        /// [Preview API] Delete a task group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId">Id of the task group to be deleted.</param>
        /// <param name="comment">Comments to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTaskGroupAsync(
            Guid project,
            Guid taskGroupId,
            string comment = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (comment != null)
            {
                queryParams.Add("comment", comment);
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
        /// [Preview API] Get task group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId">Id of the task group.</param>
        /// <param name="versionSpec">version specification of the task group. examples: 1, 1.0.</param>
        /// <param name="expand">The properties that should be expanded. example $expand=Tasks will expand nested task groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskGroup> GetTaskGroupAsync(
            string project,
            Guid taskGroupId,
            string versionSpec,
            TaskGroupExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("versionSpec", versionSpec);
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get task group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId">Id of the task group.</param>
        /// <param name="versionSpec">version specification of the task group. examples: 1, 1.0.</param>
        /// <param name="expand">The properties that should be expanded. example $expand=Tasks will expand nested task groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskGroup> GetTaskGroupAsync(
            Guid project,
            Guid taskGroupId,
            string versionSpec,
            TaskGroupExpands? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("versionSpec", versionSpec);
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<TaskGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId"></param>
        /// <param name="revision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> GetTaskGroupRevisionAsync(
            string project,
            Guid taskGroupId,
            int revision,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("revision", revision.ToString(CultureInfo.InvariantCulture));
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "text/plain",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId"></param>
        /// <param name="revision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> GetTaskGroupRevisionAsync(
            Guid project,
            Guid taskGroupId,
            int revision,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("revision", revision.ToString(CultureInfo.InvariantCulture));
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "text/plain",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API] List task groups.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId">Id of the task group.</param>
        /// <param name="expanded">'true' to recursively expand task groups. Default is 'false'.</param>
        /// <param name="taskIdFilter">Guid of the taskId to filter.</param>
        /// <param name="deleted">'true'to include deleted task groups. Default is 'false'.</param>
        /// <param name="top">Number of task groups to get.</param>
        /// <param name="continuationToken">Gets the task groups after the continuation token provided.</param>
        /// <param name="queryOrder">Gets the results in the defined order. Default is 'CreatedOnDescending'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            string project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            Guid? taskIdFilter = null,
            bool? deleted = null,
            int? top = null,
            DateTime? continuationToken = null,
            TaskGroupQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expanded != null)
            {
                queryParams.Add("expanded", expanded.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }
            if (deleted != null)
            {
                queryParams.Add("deleted", deleted.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                AddDateTimeToQueryParams(queryParams, "continuationToken", continuationToken.Value);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] List task groups.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId">Id of the task group.</param>
        /// <param name="expanded">'true' to recursively expand task groups. Default is 'false'.</param>
        /// <param name="taskIdFilter">Guid of the taskId to filter.</param>
        /// <param name="deleted">'true'to include deleted task groups. Default is 'false'.</param>
        /// <param name="top">Number of task groups to get.</param>
        /// <param name="continuationToken">Gets the task groups after the continuation token provided.</param>
        /// <param name="queryOrder">Gets the results in the defined order. Default is 'CreatedOnDescending'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            Guid project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            Guid? taskIdFilter = null,
            bool? deleted = null,
            int? top = null,
            DateTime? continuationToken = null,
            TaskGroupQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expanded != null)
            {
                queryParams.Add("expanded", expanded.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }
            if (deleted != null)
            {
                queryParams.Add("deleted", deleted.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                AddDateTimeToQueryParams(queryParams, "continuationToken", continuationToken.Value);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId"></param>
        /// <param name="taskGroup"></param>
        /// <param name="disablePriorVersions"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> PublishPreviewTaskGroupAsync(
            string project,
            Guid taskGroupId,
            TaskGroup taskGroup,
            bool? disablePriorVersions = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };
            HttpContent content = new ObjectContent<TaskGroup>(taskGroup, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (disablePriorVersions != null)
            {
                queryParams.Add("disablePriorVersions", disablePriorVersions.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId"></param>
        /// <param name="taskGroup"></param>
        /// <param name="disablePriorVersions"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> PublishPreviewTaskGroupAsync(
            Guid project,
            Guid taskGroupId,
            TaskGroup taskGroup,
            bool? disablePriorVersions = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };
            HttpContent content = new ObjectContent<TaskGroup>(taskGroup, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (disablePriorVersions != null)
            {
                queryParams.Add("disablePriorVersions", disablePriorVersions.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="parentTaskGroupId"></param>
        /// <param name="taskGroupMetadata"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> PublishTaskGroupAsync(
            string project,
            Guid parentTaskGroupId,
            PublishTaskGroupMetadata taskGroupMetadata,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<PublishTaskGroupMetadata>(taskGroupMetadata, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("parentTaskGroupId", parentTaskGroupId.ToString());

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="parentTaskGroupId"></param>
        /// <param name="taskGroupMetadata"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> PublishTaskGroupAsync(
            Guid project,
            Guid parentTaskGroupId,
            PublishTaskGroupMetadata taskGroupMetadata,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<PublishTaskGroupMetadata>(taskGroupMetadata, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("parentTaskGroupId", parentTaskGroupId.ToString());

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> UndeleteTaskGroupAsync(
            string project,
            TaskGroup taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroup>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<TaskGroup>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="taskGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskGroup>> UndeleteTaskGroupAsync(
            Guid project,
            TaskGroup taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroup>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a task group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroup">Task group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use UpdateTaskGroup(Guid taskGroupId, [FromBody] TaskGroupUpdateParameter taskGroup) instead")]
        public virtual Task<TaskGroup> UpdateTaskGroupAsync(
            string project,
            TaskGroupUpdateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroupUpdateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a task group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroup">Task group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use UpdateTaskGroup(Guid taskGroupId, [FromBody] TaskGroupUpdateParameter taskGroup) instead")]
        public virtual Task<TaskGroup> UpdateTaskGroupAsync(
            Guid project,
            TaskGroupUpdateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<TaskGroupUpdateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a task group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId">Id of the task group to update.</param>
        /// <param name="taskGroup">Task group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskGroup> UpdateTaskGroupAsync(
            string project,
            Guid taskGroupId,
            TaskGroupUpdateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };
            HttpContent content = new ObjectContent<TaskGroupUpdateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a task group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId">Id of the task group to update.</param>
        /// <param name="taskGroup">Task group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskGroup> UpdateTaskGroupAsync(
            Guid project,
            Guid taskGroupId,
            TaskGroupUpdateParameter taskGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };
            HttpContent content = new ObjectContent<TaskGroupUpdateParameter>(taskGroup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskGroup>(
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
        /// <param name="taskId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteTaskDefinitionAsync(
            Guid taskId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("60aac929-f0cd-4bc8-9ce4-6b30e8f1b1bd");
            object routeValues = new { taskId = taskId };

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
        /// <param name="taskId"></param>
        /// <param name="versionString"></param>
        /// <param name="visibility"></param>
        /// <param name="scopeLocal"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task<Stream> GetTaskContentZipAsync(
            Guid taskId,
            string versionString,
            IEnumerable<string> visibility = null,
            bool? scopeLocal = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("60aac929-f0cd-4bc8-9ce4-6b30e8f1b1bd");
            object routeValues = new { taskId = taskId, versionString = versionString };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (visibility != null)
            {
                AddIEnumerableAsQueryParams(queryParams, "visibility", visibility);
            }
            if (scopeLocal != null)
            {
                queryParams.Add("scopeLocal", scopeLocal.Value.ToString());
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "application/zip",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="versionString"></param>
        /// <param name="visibility"></param>
        /// <param name="scopeLocal"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskDefinition> GetTaskDefinitionAsync(
            Guid taskId,
            string versionString,
            IEnumerable<string> visibility = null,
            bool? scopeLocal = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("60aac929-f0cd-4bc8-9ce4-6b30e8f1b1bd");
            object routeValues = new { taskId = taskId, versionString = versionString };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (visibility != null)
            {
                AddIEnumerableAsQueryParams(queryParams, "visibility", visibility);
            }
            if (scopeLocal != null)
            {
                queryParams.Add("scopeLocal", scopeLocal.Value.ToString());
            }

            return SendAsync<TaskDefinition>(
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
        /// <param name="taskId"></param>
        /// <param name="visibility"></param>
        /// <param name="scopeLocal"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskDefinition>> GetTaskDefinitionsAsync(
            Guid? taskId = null,
            IEnumerable<string> visibility = null,
            bool? scopeLocal = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("60aac929-f0cd-4bc8-9ce4-6b30e8f1b1bd");
            object routeValues = new { taskId = taskId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (visibility != null)
            {
                AddIEnumerableAsQueryParams(queryParams, "visibility", visibility);
            }
            if (scopeLocal != null)
            {
                queryParams.Add("scopeLocal", scopeLocal.Value.ToString());
            }

            return SendAsync<List<TaskDefinition>>(
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
        /// <param name="userCapabilities"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgent> UpdateAgentUserCapabilitiesAsync(
            int poolId,
            int agentId,
            IDictionary<string, string> userCapabilities,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("30ba3ada-fedf-4da8-bbb5-dacf2f82e176");
            object routeValues = new { poolId = poolId, agentId = agentId };
            HttpContent content = new ObjectContent<IDictionary<string, string>>(userCapabilities, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskAgent>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Add a variable group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="group">Variable group to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> AddVariableGroupAsync(
            string project,
            VariableGroupParameters group,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<VariableGroupParameters>(group, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VariableGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Add a variable group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="group">Variable group to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> AddVariableGroupAsync(
            Guid project,
            VariableGroupParameters group,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<VariableGroupParameters>(group, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VariableGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete a variable group
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="groupId">Id of the variable group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteVariableGroupAsync(
            string project,
            int groupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };

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
        /// [Preview API] Delete a variable group
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="groupId">Id of the variable group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteVariableGroupAsync(
            Guid project,
            int groupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };

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
        /// [Preview API] Get a variable group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="groupId">Id of the variable group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> GetVariableGroupAsync(
            string project,
            int groupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };

            return SendAsync<VariableGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a variable group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="groupId">Id of the variable group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> GetVariableGroupAsync(
            Guid project,
            int groupId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };

            return SendAsync<VariableGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get variable groups.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="groupName">Name of variable group.</param>
        /// <param name="actionFilter">Action filter for the variable group. It specifies the action which can be performed on the variable groups.</param>
        /// <param name="top">Number of variable groups to get.</param>
        /// <param name="continuationToken">Gets the variable groups after the continuation token provided.</param>
        /// <param name="queryOrder">Gets the results in the defined order. Default is 'IdDescending'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<VariableGroup>> GetVariableGroupsAsync(
            string project,
            string groupName = null,
            VariableGroupActionFilter? actionFilter = null,
            int? top = null,
            int? continuationToken = null,
            VariableGroupQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (groupName != null)
            {
                queryParams.Add("groupName", groupName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<VariableGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get variable groups.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="groupName">Name of variable group.</param>
        /// <param name="actionFilter">Action filter for the variable group. It specifies the action which can be performed on the variable groups.</param>
        /// <param name="top">Number of variable groups to get.</param>
        /// <param name="continuationToken">Gets the variable groups after the continuation token provided.</param>
        /// <param name="queryOrder">Gets the results in the defined order. Default is 'IdDescending'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<VariableGroup>> GetVariableGroupsAsync(
            Guid project,
            string groupName = null,
            VariableGroupActionFilter? actionFilter = null,
            int? top = null,
            int? continuationToken = null,
            VariableGroupQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (groupName != null)
            {
                queryParams.Add("groupName", groupName);
            }
            if (actionFilter != null)
            {
                queryParams.Add("actionFilter", actionFilter.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<VariableGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get variable groups by ids.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="groupIds">Comma separated list of Ids of variable groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<VariableGroup>> GetVariableGroupsByIdAsync(
            string project,
            IEnumerable<int> groupIds,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string groupIdsAsString = null;
            if (groupIds != null)
            {
                groupIdsAsString = string.Join(",", groupIds);
            }
            queryParams.Add("groupIds", groupIdsAsString);

            return SendAsync<List<VariableGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get variable groups by ids.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="groupIds">Comma separated list of Ids of variable groups.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<VariableGroup>> GetVariableGroupsByIdAsync(
            Guid project,
            IEnumerable<int> groupIds,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string groupIdsAsString = null;
            if (groupIds != null)
            {
                groupIdsAsString = string.Join(",", groupIds);
            }
            queryParams.Add("groupIds", groupIdsAsString);

            return SendAsync<List<VariableGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Update a variable group.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="groupId">Id of the variable group to update.</param>
        /// <param name="group">Variable group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> UpdateVariableGroupAsync(
            string project,
            int groupId,
            VariableGroupParameters group,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };
            HttpContent content = new ObjectContent<VariableGroupParameters>(group, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VariableGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Update a variable group.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="groupId">Id of the variable group to update.</param>
        /// <param name="group">Variable group to update.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<VariableGroup> UpdateVariableGroupAsync(
            Guid project,
            int groupId,
            VariableGroupParameters group,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("f5b09dd5-9d54-45a1-8b5a-1c8287d634cc");
            object routeValues = new { project = project, groupId = groupId };
            HttpContent content = new ObjectContent<VariableGroupParameters>(group, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VariableGroup>(
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
        /// <param name="groupId"></param>
        /// <param name="project"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<ProjectReference>> QuerySharedProjectsForVariableGroupAsync(
            int groupId,
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("74455598-def7-499a-b7a3-a41d1c8225f8");
            object routeValues = new { groupId = groupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("project", project);

            return SendAsync<List<ProjectReference>>(
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
        /// <param name="groupId"></param>
        /// <param name="fromProject"></param>
        /// <param name="withProject"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task ShareVariableGroupWithProjectAsync(
            int groupId,
            string fromProject,
            string withProject,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("74455598-def7-499a-b7a3-a41d1c8225f8");
            object routeValues = new { groupId = groupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("fromProject", fromProject);
            queryParams.Add("withProject", withProject);

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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="createParameters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> AddVirtualMachineGroupAsync(
            string project,
            int environmentId,
            VirtualMachineGroupCreateParameters createParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<VirtualMachineGroupCreateParameters>(createParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VirtualMachineGroup>(
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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="createParameters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> AddVirtualMachineGroupAsync(
            Guid project,
            int environmentId,
            VirtualMachineGroupCreateParameters createParameters,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<VirtualMachineGroupCreateParameters>(createParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VirtualMachineGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteVirtualMachineGroupAsync(
            string project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task DeleteVirtualMachineGroupAsync(
            Guid project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> GetVirtualMachineGroupAsync(
            string project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            return SendAsync<VirtualMachineGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> GetVirtualMachineGroupAsync(
            Guid project,
            int environmentId,
            int resourceId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            return SendAsync<VirtualMachineGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resource"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> UpdateVirtualMachineGroupAsync(
            string project,
            int environmentId,
            VirtualMachineGroup resource,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<VirtualMachineGroup>(resource, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VirtualMachineGroup>(
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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resource"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<VirtualMachineGroup> UpdateVirtualMachineGroupAsync(
            Guid project,
            int environmentId,
            VirtualMachineGroup resource,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("9e597901-4af7-4cc3-8d92-47d54db8ebfb");
            object routeValues = new { project = project, environmentId = environmentId };
            HttpContent content = new ObjectContent<VirtualMachineGroup>(resource, new VssJsonMediaTypeFormatter(true));

            return SendAsync<VirtualMachineGroup>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="name"></param>
        /// <param name="partialNameMatch"></param>
        /// <param name="tags"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<VirtualMachine>> GetVirtualMachinesAsync(
            string project,
            int environmentId,
            int resourceId,
            string continuationToken = null,
            string name = null,
            bool? partialNameMatch = null,
            IEnumerable<string> tags = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("48700676-2ba5-4282-8ec8-083280d169c7");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (partialNameMatch != null)
            {
                queryParams.Add("partialNameMatch", partialNameMatch.Value.ToString());
            }
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<VirtualMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="name"></param>
        /// <param name="partialNameMatch"></param>
        /// <param name="tags"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<VirtualMachine>> GetVirtualMachinesAsync(
            Guid project,
            int environmentId,
            int resourceId,
            string continuationToken = null,
            string name = null,
            bool? partialNameMatch = null,
            IEnumerable<string> tags = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("48700676-2ba5-4282-8ec8-083280d169c7");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (partialNameMatch != null)
            {
                queryParams.Add("partialNameMatch", partialNameMatch.Value.ToString());
            }
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<VirtualMachine>>(
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="machines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<VirtualMachine>> UpdateVirtualMachinesAsync(
            string project,
            int environmentId,
            int resourceId,
            IEnumerable<VirtualMachine> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("48700676-2ba5-4282-8ec8-083280d169c7");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };
            HttpContent content = new ObjectContent<IEnumerable<VirtualMachine>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<VirtualMachine>>(
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
        /// <param name="project">Project ID</param>
        /// <param name="environmentId"></param>
        /// <param name="resourceId"></param>
        /// <param name="machines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<VirtualMachine>> UpdateVirtualMachinesAsync(
            Guid project,
            int environmentId,
            int resourceId,
            IEnumerable<VirtualMachine> machines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("48700676-2ba5-4282-8ec8-083280d169c7");
            object routeValues = new { project = project, environmentId = environmentId, resourceId = resourceId };
            HttpContent content = new ObjectContent<IEnumerable<VirtualMachine>>(machines, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<VirtualMachine>>(
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
        /// <param name="authenticationRequest"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<AadOauthTokenResult> AcquireAccessTokenAsync(
            AadOauthTokenRequest authenticationRequest,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9c63205e-3a0f-42a0-ad88-095200f13607");
            HttpContent content = new ObjectContent<AadOauthTokenRequest>(authenticationRequest, new VssJsonMediaTypeFormatter(true));

            return SendAsync<AadOauthTokenResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="promptOption"></param>
        /// <param name="completeCallbackPayload"></param>
        /// <param name="completeCallbackByAuthCode"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ServiceEndpoint APIs under distributedtask area is deprecated. Use the APIs under serviceendpoint area instead.")]
        public virtual Task<string> CreateAadOAuthRequestAsync(
            string tenantId,
            string redirectUri,
            AadLoginPromptOption? promptOption = null,
            string completeCallbackPayload = null,
            bool? completeCallbackByAuthCode = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9c63205e-3a0f-42a0-ad88-095200f13607");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("tenantId", tenantId);
            queryParams.Add("redirectUri", redirectUri);
            if (promptOption != null)
            {
                queryParams.Add("promptOption", promptOption.Value.ToString());
            }
            if (completeCallbackPayload != null)
            {
                queryParams.Add("completeCallbackPayload", completeCallbackPayload);
            }
            if (completeCallbackByAuthCode != null)
            {
                queryParams.Add("completeCallbackByAuthCode", completeCallbackByAuthCode.Value.ToString());
            }

            return SendAsync<string>(
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> GetVstsAadTenantIdAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("9c63205e-3a0f-42a0-ad88-095200f13607");

            return SendAsync<string>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Object> GetYamlSchemaAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1f9990b9-1dba-441f-9c2e-6485888c42b6");

            return SendAsync<Object>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
