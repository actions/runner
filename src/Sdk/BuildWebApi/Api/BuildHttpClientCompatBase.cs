using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    public abstract class BuildHttpClientCompatBase: VssHttpClientBase
    {
        public BuildHttpClientCompatBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public BuildHttpClientCompatBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BuildHttpClientCompatBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BuildHttpClientCompatBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BuildHttpClientCompatBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        [Obsolete]
        public virtual Task<List<Build>> GetBuildsAsync(
            string project,
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minFinishTime = null,
            DateTime? maxFinishTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            IEnumerable<string> tagFilters = null,
            IEnumerable<string> properties = null,
            int? top = null,
            string continuationToken = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitions != null && definitions.Any())
            {
                queryParams.Add("definitions", string.Join(",", definitions));
            }
            if (queues != null && queues.Any())
            {
                queryParams.Add("queues", string.Join(",", queues));
            }
            if (!string.IsNullOrEmpty(buildNumber))
            {
                queryParams.Add("buildNumber", buildNumber);
            }
            if (minFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minFinishTime", minFinishTime.Value);
            }
            if (maxFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "maxFinishTime", maxFinishTime.Value);
            }
            if (!string.IsNullOrEmpty(requestedFor))
            {
                queryParams.Add("requestedFor", requestedFor);
            }
            if (reasonFilter != null)
            {
                queryParams.Add("reasonFilter", reasonFilter.Value.ToString());
            }
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (resultFilter != null)
            {
                queryParams.Add("resultFilter", resultFilter.Value.ToString());
            }
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (maxBuildsPerDefinition != null)
            {
                queryParams.Add("maxBuildsPerDefinition", maxBuildsPerDefinition.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (deletedFilter != null)
            {
                queryParams.Add("deletedFilter", deletedFilter.Value.ToString());
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (!string.IsNullOrEmpty(branchName))
            {
                queryParams.Add("branchName", branchName);
            }
            if (buildIds != null && buildIds.Any())
            {
                queryParams.Add("buildIds", string.Join(",", buildIds));
            }

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Queues a build
        /// </summary>
        /// <param name="build"></param>
        /// <param name="project">Project ID</param>
        /// <param name="ignoreWarnings"></param>
        /// <param name="checkInTicket"></param>
        /// <param name="sourceBuildId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            Guid project,
            bool? ignoreWarnings = null,
            string checkInTicket = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ignoreWarnings != null)
            {
                queryParams.Add("ignoreWarnings", ignoreWarnings.Value.ToString());
            }
            if (!string.IsNullOrEmpty(checkInTicket))
            {
                queryParams.Add("checkInTicket", checkInTicket);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.0-preview.4"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Queues a build
        /// </summary>
        /// <param name="build"></param>
        /// <param name="ignoreWarnings"></param>
        /// <param name="checkInTicket"></param>
        /// <param name="sourceBuildId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            bool? ignoreWarnings = null,
            string checkInTicket = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ignoreWarnings != null)
            {
                queryParams.Add("ignoreWarnings", ignoreWarnings.Value.ToString());
            }
            if (!string.IsNullOrEmpty(checkInTicket))
            {
                queryParams.Add("checkInTicket", checkInTicket);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.0-preview.4"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Queues a build
        /// </summary>
        /// <param name="build"></param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="ignoreWarnings"></param>
        /// <param name="checkInTicket"></param>
        /// <param name="sourceBuildId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            string project,
            bool? ignoreWarnings = null,
            string checkInTicket = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ignoreWarnings != null)
            {
                queryParams.Add("ignoreWarnings", ignoreWarnings.Value.ToString());
            }
            if (!string.IsNullOrEmpty(checkInTicket))
            {
                queryParams.Add("checkInTicket", checkInTicket);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.0-preview.4"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets builds
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitions">A comma-delimited list of definition ids</param>
        /// <param name="queues">A comma-delimited list of queue ids</param>
        /// <param name="buildNumber"></param>
        /// <param name="minFinishTime"></param>
        /// <param name="maxFinishTime"></param>
        /// <param name="requestedFor"></param>
        /// <param name="reasonFilter"></param>
        /// <param name="statusFilter"></param>
        /// <param name="resultFilter"></param>
        /// <param name="tagFilters">A comma-delimited list of tags</param>
        /// <param name="properties">A comma-delimited list of properties to include in the results</param>
        /// <param name="top">The maximum number of builds to retrieve</param>
        /// <param name="continuationToken"></param>
        /// <param name="maxBuildsPerDefinition"></param>
        /// <param name="deletedFilter"></param>
        /// <param name="queryOrder"></param>
        /// <param name="branchName"></param>
        /// <param name="buildIds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> GetBuildsAsync(
            Guid project,
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minFinishTime = null,
            DateTime? maxFinishTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            IEnumerable<string> tagFilters = null,
            IEnumerable<string> properties = null,
            int? top = null,
            string continuationToken = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitions != null && definitions.Any())
            {
                queryParams.Add("definitions", string.Join(",", definitions));
            }
            if (queues != null && queues.Any())
            {
                queryParams.Add("queues", string.Join(",", queues));
            }
            if (!string.IsNullOrEmpty(buildNumber))
            {
                queryParams.Add("buildNumber", buildNumber);
            }
            if (minFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minFinishTime", minFinishTime.Value);
            }
            if (maxFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "maxFinishTime", maxFinishTime.Value);
            }
            if (!string.IsNullOrEmpty(requestedFor))
            {
                queryParams.Add("requestedFor", requestedFor);
            }
            if (reasonFilter != null)
            {
                queryParams.Add("reasonFilter", reasonFilter.Value.ToString());
            }
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (resultFilter != null)
            {
                queryParams.Add("resultFilter", resultFilter.Value.ToString());
            }
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (maxBuildsPerDefinition != null)
            {
                queryParams.Add("maxBuildsPerDefinition", maxBuildsPerDefinition.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (deletedFilter != null)
            {
                queryParams.Add("deletedFilter", deletedFilter.Value.ToString());
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (!string.IsNullOrEmpty(branchName))
            {
                queryParams.Add("branchName", branchName);
            }
            if (buildIds != null && buildIds.Any())
            {
                queryParams.Add("buildIds", string.Join(",", buildIds));
            }

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets builds
        /// </summary>
        /// <param name="definitions">A comma-delimited list of definition ids</param>
        /// <param name="queues">A comma-delimited list of queue ids</param>
        /// <param name="buildNumber"></param>
        /// <param name="minFinishTime"></param>
        /// <param name="maxFinishTime"></param>
        /// <param name="requestedFor"></param>
        /// <param name="reasonFilter"></param>
        /// <param name="statusFilter"></param>
        /// <param name="resultFilter"></param>
        /// <param name="tagFilters">A comma-delimited list of tags</param>
        /// <param name="properties">A comma-delimited list of properties to include in the results</param>
        /// <param name="top">The maximum number of builds to retrieve</param>
        /// <param name="continuationToken"></param>
        /// <param name="maxBuildsPerDefinition"></param>
        /// <param name="deletedFilter"></param>
        /// <param name="queryOrder"></param>
        /// <param name="branchName"></param>
        /// <param name="buildIds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> GetBuildsAsync(
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minFinishTime = null,
            DateTime? maxFinishTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            IEnumerable<string> tagFilters = null,
            IEnumerable<string> properties = null,
            int? top = null,
            string continuationToken = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitions != null && definitions.Any())
            {
                queryParams.Add("definitions", string.Join(",", definitions));
            }
            if (queues != null && queues.Any())
            {
                queryParams.Add("queues", string.Join(",", queues));
            }
            if (!string.IsNullOrEmpty(buildNumber))
            {
                queryParams.Add("buildNumber", buildNumber);
            }
            if (minFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minFinishTime", minFinishTime.Value);
            }
            if (maxFinishTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "maxFinishTime", maxFinishTime.Value);
            }
            if (!string.IsNullOrEmpty(requestedFor))
            {
                queryParams.Add("requestedFor", requestedFor);
            }
            if (reasonFilter != null)
            {
                queryParams.Add("reasonFilter", reasonFilter.Value.ToString());
            }
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (resultFilter != null)
            {
                queryParams.Add("resultFilter", resultFilter.Value.ToString());
            }
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (maxBuildsPerDefinition != null)
            {
                queryParams.Add("maxBuildsPerDefinition", maxBuildsPerDefinition.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (deletedFilter != null)
            {
                queryParams.Add("deletedFilter", deletedFilter.Value.ToString());
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (!string.IsNullOrEmpty(branchName))
            {
                queryParams.Add("branchName", branchName);
            }
            if (buildIds != null && buildIds.Any())
            {
                queryParams.Add("buildIds", string.Join(",", buildIds));
            }

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a definition, optionally at a specific revision
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId"></param>
        /// <param name="revision"></param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> GetDefinitionAsync(
            string project,
            int definitionId,
            int? revision = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a definition, optionally at a specific revision
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId"></param>
        /// <param name="revision"></param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> GetDefinitionAsync(
            Guid project,
            int definitionId,
            int? revision = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a definition, optionally at a specific revision
        /// </summary>
        /// <param name="definitionId"></param>
        /// <param name="revision"></param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetDefinitionAsync(string, int) instead.")]
        public virtual Task<BuildDefinition> GetDefinitionAsync(
            int definitionId,
            int? revision = null,
            IEnumerable<string> propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetDefinitionsAsync(string) instead.")]
        public virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="includeAllProperties"></param>
        /// <param name="includeLatestBuilds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="includeAllProperties"></param>
        /// <param name="includeLatestBuilds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTime"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="builtAfter"></param>
        /// <param name="notBuiltAfter"></param>
        /// <param name="includeAllProperties"></param>
        /// <param name="includeLatestBuilds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetDefinitionsAsync(string) instead.")]
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of definitions.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name">If specified, filters to definitions whose names match this pattern.</param>
        /// <param name="repositoryId">A repository ID. If specified, filters to definitions that use this repository.</param>
        /// <param name="repositoryType">If specified, filters to definitions that have a repository of this type.</param>
        /// <param name="queryOrder">Indicates the order in which definitions should be returned.</param>
        /// <param name="top">The maximum number of definitions to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of definitions.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="definitionIds">A comma-delimited list that specifies the IDs of definitions to retrieve.</param>
        /// <param name="path">If specified, filters to definitions under this folder.</param>
        /// <param name="builtAfter">If specified, filters to definitions that have builds after this date.</param>
        /// <param name="notBuiltAfter">If specified, filters to definitions that do not have builds after this date.</param>
        /// <param name="includeAllProperties">Indicates whether the full definitions should be returned. By default, shallow representations of the definitions are returned.</param>
        /// <param name="includeLatestBuilds">Indicates whether to return the latest and latest completed builds for this definition.</param>
        /// <param name="taskIdFilter">If specified, filters to definitions that use the specified task.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.0-preview.6"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of definitions.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name">If specified, filters to definitions whose names match this pattern.</param>
        /// <param name="repositoryId">A repository ID. If specified, filters to definitions that use this repository.</param>
        /// <param name="repositoryType">If specified, filters to definitions that have a repository of this type.</param>
        /// <param name="queryOrder">Indicates the order in which definitions should be returned.</param>
        /// <param name="top">The maximum number of definitions to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of definitions.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="definitionIds">A comma-delimited list that specifies the IDs of definitions to retrieve.</param>
        /// <param name="path">If specified, filters to definitions under this folder.</param>
        /// <param name="builtAfter">If specified, filters to definitions that have builds after this date.</param>
        /// <param name="notBuiltAfter">If specified, filters to definitions that do not have builds after this date.</param>
        /// <param name="includeAllProperties">Indicates whether the full definitions should be returned. By default, shallow representations of the definitions are returned.</param>
        /// <param name="includeLatestBuilds">Indicates whether to return the latest and latest completed builds for this definition.</param>
        /// <param name="taskIdFilter">If specified, filters to definitions that use the specified task.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.0-preview.6"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of definitions.
        /// </summary>
        /// <param name="name">If specified, filters to definitions whose names match this pattern.</param>
        /// <param name="repositoryId">A repository ID. If specified, filters to definitions that use this repository.</param>
        /// <param name="repositoryType">If specified, filters to definitions that have a repository of this type.</param>
        /// <param name="queryOrder">Indicates the order in which definitions should be returned.</param>
        /// <param name="top">The maximum number of definitions to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of definitions.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="definitionIds">A comma-delimited list that specifies the IDs of definitions to retrieve.</param>
        /// <param name="path">If specified, filters to definitions under this folder.</param>
        /// <param name="builtAfter">If specified, filters to definitions that have builds after this date.</param>
        /// <param name="notBuiltAfter">If specified, filters to definitions that do not have builds after this date.</param>
        /// <param name="includeAllProperties">Indicates whether the full definitions should be returned. By default, shallow representations of the definitions are returned.</param>
        /// <param name="includeLatestBuilds">Indicates whether to return the latest and latest completed builds for this definition.</param>
        /// <param name="taskIdFilter">If specified, filters to definitions that use the specified task.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.0-preview.6"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<BuildDefinitionReference>> GetDefinitionsAsync2(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                false);

            return SendAsync<IPagedList<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinitionReference>
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<BuildDefinitionReference>> GetDefinitionsAsync2(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                false);

            return SendAsync<IPagedList<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinitionReference>
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetDefinitionsAsync2(string) instead.")]
        public virtual Task<IPagedList<BuildDefinitionReference>> GetDefinitionsAsync2(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                false);

            return SendAsync<IPagedList<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinitionReference>
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinition>> GetFullDefinitionsAsync(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<List<BuildDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinition>> GetFullDefinitionsAsync(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<List<BuildDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetFullDefinitionsAsync(string) instead.")]
        public virtual Task<List<BuildDefinition>> GetFullDefinitionsAsync(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<List<BuildDefinition>>(
                httpMethod,
                locationId,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<BuildDefinition>> GetFullDefinitionsAsync2(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<IPagedList<BuildDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinition>
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<BuildDefinition>> GetFullDefinitionsAsync2(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<IPagedList<BuildDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinition>
            );
        }

        /// <summary>
        /// [Preview API] Gets definitions, optionally filtered by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repositoryId"></param>
        /// <param name="repositoryType"></param>
        /// <param name="queryOrder"></param>
        /// <param name="top"></param>
        /// <param name="continuationToken"></param>
        /// <param name="minMetricsTimeInUtc"></param>
        /// <param name="definitionIds"></param>
        /// <param name="path"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use GetFullDefinitionsAsync2(string) instead.")]
        public virtual Task<IPagedList<BuildDefinition>> GetFullDefinitionsAsync2(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");

            List<KeyValuePair<string, string>> queryParams = GetDefinitionsQueryParams(
                name,
                repositoryId,
                repositoryType,
                queryOrder,
                top,
                continuationToken,
                minMetricsTimeInUtc,
                definitionIds,
                path,
                builtAfter,
                notBuiltAfter,
                true);

            return SendAsync<IPagedList<BuildDefinition>>(
                httpMethod,
                locationId,
                version: s_DefinitionsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<BuildDefinition>
            );
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use UpdateBuildAsync(Build, bool, object, CancellationToken) instead.")]
        public virtual Task<Build> UpdateBuildAsync(
            Build build,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { buildId = buildId };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.0, 4),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use UpdateBuildAsync(Build, bool, object, CancellationToken) instead.")]
        public virtual Task<Build> UpdateBuildAsync(
            Build build,
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.0, 4),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use UpdateBuildAsync(Build, bool, object, CancellationToken) instead.")]
        public virtual Task<Build> UpdateBuildAsync(
            Build build,
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.0, 4),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets a list of branches for the given source code repository.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> ListBranchesAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e05d4403-9b81-4244-8763-20fde28d1976");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of branches for the given source code repository.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> ListBranchesAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e05d4403-9b81-4244-8763-20fde28d1976");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        private protected List<KeyValuePair<string, string>> GetDefinitionsQueryParams(
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTimeInUtc = null,
            IEnumerable<int> definitionIds = null,
            String path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null)
        {
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

            // in case the server doesn't support 3.x yet
            queryParams.Add("type", DefinitionType.Build.ToString());

            if (!string.IsNullOrEmpty(name))
            {
                queryParams.Add("name", name);
            }
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTimeInUtc != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTimeInUtc", minMetricsTimeInUtc.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (!string.IsNullOrEmpty(path))
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties ?? false)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds ?? false)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter.HasValue)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }
            if (processType.HasValue)
            {
                queryParams.Add("processType", processType.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(yamlFilename))
            {
                queryParams.Add("yamlFilename", yamlFilename);
            }

            return queryParams;
        }

        private protected async Task<IPagedList<T>> GetPagedList<T>(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            var continuationToken = GetContinuationToken(responseMessage);
            var list = await ReadContentAsAsync<List<T>>(responseMessage, cancellationToken).ConfigureAwait(false);
            return new PagedList<T>(list, continuationToken);
        }

        private protected Task<T> SendAsync<T>(
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

        private protected async Task<T> SendAsync<T>(
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

        private protected async Task<T> SendAsync<T>(
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

        private protected string GetContinuationToken(HttpResponseMessage responseMessage)
        {
            string continuationToken = null;

            IEnumerable<string> headerValues = null;
            if (responseMessage.Headers.TryGetValues("x-ms-continuationtoken", out headerValues))
            {
                continuationToken = headerValues.FirstOrDefault();
            }

            return continuationToken;
        }

        protected static readonly ApiResourceVersion s_BuildsApiVersion = new ApiResourceVersion("4.1-preview.3");
        protected static readonly ApiResourceVersion s_DefinitionsApiVersion = new ApiResourceVersion("4.1-preview.6");
    }
}
