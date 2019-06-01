using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public abstract class TaskAgentHttpClientCompatBase : VssHttpClientBase
    {
        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTaskGroupAsync(
            string project,
            Guid taskGroupId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
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
        /// <param name="taskGroupId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTaskGroupAsync(
            Guid project,
            Guid taskGroupId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
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
        /// <param name="taskGroupId"></param>
        /// <param name="expanded"></param>
        /// <param name="taskIdFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            string project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            Guid? taskIdFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
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

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId"></param>
        /// <param name="expanded"></param>
        /// <param name="taskIdFilter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            Guid project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            Guid? taskIdFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
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

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="taskGroupId"></param>
        /// <param name="expanded"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            string project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expanded != null)
            {
                queryParams.Add("expanded", expanded.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("3.2-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="taskGroupId"></param>
        /// <param name="expanded"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskGroup>> GetTaskGroupsAsync(
            Guid project,
            Guid? taskGroupId = null,
            bool? expanded = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6c08ffbf-dbf1-4f9a-94e5-a1cbd47005e7");
            object routeValues = new { project = project, taskGroupId = taskGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (expanded != null)
            {
                queryParams.Add("expanded", expanded.Value.ToString());
            }

            return SendAsync<List<TaskGroup>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("3.2-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get information about an agent.
        /// </summary>
        /// <param name="poolId">The agent pool containing the agent</param>
        /// <param name="agentId">The agent ID to get information about</param>
        /// <param name="includeCapabilities">Whether to include the agent's capabilities in the response</param>
        /// <param name="includeAssignedRequest">Whether to include details about the agent's current work</param>
        /// <param name="propertyFilters">Filter which custom properties will be returned</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete]
        public virtual Task<TaskAgent> GetAgentAsync(
            int poolId,
            int agentId,
            bool? includeCapabilities,
            bool? includeAssignedRequest,
            IEnumerable<string> propertyFilters,
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
        /// <param name="propertyFilters">Filter which custom properties will be returned</param>
        /// <param name="demands">Filter by demands the agents can satisfy</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete]
        public virtual Task<List<TaskAgent>> GetAgentsAsync(
            int poolId,
            string agentName,
            bool? includeCapabilities,
            bool? includeAssignedRequest,
            IEnumerable<string> propertyFilters,
            IEnumerable<string> demands,
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
    }
}
