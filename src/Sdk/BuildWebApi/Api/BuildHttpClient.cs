using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Patch;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Build.WebApi
{
    public class BuildHttpClient : BuildHttpClientBase
    {
        static BuildHttpClient()
        {
        }

        public BuildHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public BuildHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BuildHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BuildHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BuildHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Creates a new definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="definitionToCloneId"></param>
        /// <param name="definitionToCloneRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        // this is just a convenient helper that uses the project id from the definition to call the API
        public virtual Task<BuildDefinition> CreateDefinitionAsync(
            BuildDefinition definition,
            Int32? definitionToCloneId = null,
            Int32? definitionToCloneRevision = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(definition, "definition");
            ArgumentUtility.CheckForNull(definition.Project, "definition.Project");
            return base.CreateDefinitionAsync(definition, definition.Project.Id, definitionToCloneId, definitionToCloneRevision, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing definition.
        /// </summary>
        /// <param name="definition">The new version of the defintion.</param>
        /// <param name="secretsSourceDefinitionId"></param>
        /// <param name="secretsSourceDefinitionRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        // this is just a convenient helper that uses the project id from the definition to call the API
        public Task<BuildDefinition> UpdateDefinitionAsync(
            BuildDefinition definition,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.UpdateDefinitionAsync(definition, definition.Project.Id, definition.Id, null, null, userState, cancellationToken);
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
        // this is just a convenient helper that uses the project id from the build to call the API
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            Boolean? ignoreWarnings = null,
            String checkInTicket = null,
            Int32? sourceBuildId = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(build, "build");
            ArgumentUtility.CheckForNull(build.Project, "build.Project");
            return base.QueueBuildAsync(build, build.Project.Id, ignoreWarnings, checkInTicket, sourceBuildId, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> UpdateBuildAsync(
            Build build,
            bool? retry = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // the client generator produces methods with projectId and buildId parameters
            // since we can get those from the build, they're redundant and potentially ambiguous (who wins?)
            // so we generate it with protected access and provide this version that only accepts the Build
            ArgumentUtility.CheckForNull(build, "build");
            ArgumentUtility.CheckForNull(build.Project, "build.Project");
            return base.UpdateBuildAsync(build, build.Project.Id, build.Id, retry, userState, cancellationToken);
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
        public virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetDefinitionsAsync(
                project,
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
                false, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename,
                userState,
                cancellationToken);
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
        public virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetDefinitionsAsync(
                project,
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
                false, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename,
                userState,
                cancellationToken);
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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                false, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                false, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                false, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
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
                true, // includeAllProperties
                includeLatestBuilds,
                taskIdFilter,
                processType,
                yamlFilename);

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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public override Task<List<Build>> GetBuildsAsync(
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
            string repositoryId = null,
            string repositoryType = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetBuildsQueryParams(
                definitions: definitions,
                queues: queues,
                buildNumber: buildNumber,
                minTime: minFinishTime,
                maxTime: maxFinishTime,
                requestedFor: requestedFor,
                reasonFilter: reasonFilter,
                statusFilter: statusFilter,
                resultFilter: resultFilter,
                tagFilters: tagFilters,
                properties: properties,
                top: top,
                continuationToken: continuationToken,
                maxBuildsPerDefinition: maxBuildsPerDefinition,
                deletedFilter: deletedFilter,
                queryOrder: queryOrder,
                branchName: branchName,
                buildIds: buildIds,
                repositoryId: repositoryId,
                repositoryType: repositoryType,
                userState: userState,
                cancellationToken: cancellationToken);

            return SendAsync<List<Build>>(
                httpMethod,
                s_getBuildsLocationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken
            );
        }

        public override Task<List<Build>> GetBuildsAsync(
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
            string repositoryId = null,
            string repositoryType = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetBuildsQueryParams(
                definitions: definitions,
                queues: queues,
                buildNumber: buildNumber,
                minTime: minFinishTime,
                maxTime: maxFinishTime,
                requestedFor: requestedFor,
                reasonFilter: reasonFilter,
                statusFilter: statusFilter,
                resultFilter: resultFilter,
                tagFilters: tagFilters,
                properties: properties,
                top: top,
                continuationToken: continuationToken,
                maxBuildsPerDefinition: maxBuildsPerDefinition,
                deletedFilter: deletedFilter,
                queryOrder: queryOrder,
                branchName: branchName,
                buildIds: buildIds,
                repositoryId: repositoryId,
                repositoryType: repositoryType,
                userState: userState,
                cancellationToken: cancellationToken);

            return SendAsync<List<Build>>(
                httpMethod,
                s_getBuildsLocationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken
            );
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<Build>> GetBuildsAsync2(
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
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetBuildsQueryParams(
                definitions: definitions,
                queues: queues,
                buildNumber: buildNumber,
                minTime: minFinishTime,
                maxTime: maxFinishTime,
                requestedFor: requestedFor,
                reasonFilter: reasonFilter,
                statusFilter: statusFilter,
                resultFilter: resultFilter,
                tagFilters: tagFilters,
                properties: properties,
                top: top,
                continuationToken: continuationToken,
                maxBuildsPerDefinition: maxBuildsPerDefinition,
                deletedFilter: deletedFilter,
                queryOrder: queryOrder,
                branchName: branchName,
                buildIds: buildIds,
                userState: userState,
                cancellationToken: cancellationToken);

            return SendAsync<IPagedList<Build>>(
                httpMethod,
                s_getBuildsLocationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<Build>
            );
        }

        public virtual Task<IPagedList<Build>> GetBuildsAsync2(
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
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = GetBuildsQueryParams(
                definitions: definitions,
                queues: queues,
                buildNumber: buildNumber,
                minTime: minFinishTime,
                maxTime: maxFinishTime,
                requestedFor: requestedFor,
                reasonFilter: reasonFilter,
                statusFilter: statusFilter,
                resultFilter: resultFilter,
                tagFilters: tagFilters,
                properties: properties,
                top: top,
                continuationToken: continuationToken,
                maxBuildsPerDefinition: maxBuildsPerDefinition,
                deletedFilter: deletedFilter,
                queryOrder: queryOrder,
                branchName: branchName,
                buildIds: buildIds,
                userState: userState,
                cancellationToken: cancellationToken);

            return SendAsync<IPagedList<Build>>(
                httpMethod,
                s_getBuildsLocationId,
                routeValues: routeValues,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<Build>
            );
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<Build>> GetBuildsAsync2(
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

            List<KeyValuePair<string, string>> queryParams = GetBuildsQueryParams(
                definitions: definitions,
                queues: queues,
                buildNumber: buildNumber,
                minTime: minFinishTime,
                maxTime: maxFinishTime,
                requestedFor: requestedFor,
                reasonFilter: reasonFilter,
                statusFilter: statusFilter,
                resultFilter: resultFilter,
                tagFilters: tagFilters,
                properties: properties,
                top: top,
                continuationToken: continuationToken,
                maxBuildsPerDefinition: maxBuildsPerDefinition,
                deletedFilter: deletedFilter,
                queryOrder: queryOrder,
                branchName: branchName,
                buildIds: buildIds,
                userState: userState,
                cancellationToken: cancellationToken);

            return SendAsync<IPagedList<Build>>(
                httpMethod,
                s_getBuildsLocationId,
                version: s_BuildsApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<Build>
            );
        }


        /// <summary>
        /// [Preview API] The changes associated with a build
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top">The maximum number of changes to return</param>
        /// <param name="includeSourceChange"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<Change>> GetBuildChangesAsync2(
            string project,
            int buildId,
            string continuationToken = null,
            int? top = null,
            bool? includeSourceChange = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54572c7b-bbd3-45d4-80dc-28be08941620");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (includeSourceChange != null)
            {
                queryParams.Add("includeSourceChange", includeSourceChange.Value.ToString());
            }

            return SendAsync<IPagedList<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_ChangesApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<Change>);
        }

        /// <summary>
        /// [Preview API] The changes associated with a build
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top">The maximum number of changes to return</param>
        /// <param name="includeSourceChange"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<Change>> GetBuildChangesAsync2(
            Guid project,
            int buildId,
            string continuationToken = null,
            int? top = null,
            bool? includeSourceChange = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54572c7b-bbd3-45d4-80dc-28be08941620");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (includeSourceChange != null)
            {
                queryParams.Add("includeSourceChange", includeSourceChange.Value.ToString());
            }

            return SendAsync<IPagedList<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: s_ChangesApiVersion,
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<Change>);
        }

        // this method does a compat check to see whether the server uses "minFinishTime" and "maxFinishTime" vs. "minTime" and "maxTime"
        private List<KeyValuePair<string, string>> GetBuildsQueryParams(
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minTime = null,
            DateTime? maxTime = null,
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
            string repositoryId = null,
            string repositoryType = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

            // default to false to keep the existing behavior
            Boolean version4_1IsNotAvailable = true;

            // get latest version available on server
            // note we could await here and change all methods to async, however just for this one call, async/await overhead is probably not worth it, reconsider if we have more async calls
            ApiResourceLocation location = GetResourceLocationAsync(s_getBuildsLocationId, userState, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
            if (location != null)
            {
                if (location.MaxVersion >= s_BuildsApiVersion.ApiVersion)
                {
                    // server has client's requested version 4.1 or greater
                    version4_1IsNotAvailable = false;
                }
            }

            // in case the server doesn't support 3.x yet
            queryParams.Add("type", DefinitionType.Build.ToString());

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

            if (version4_1IsNotAvailable)
            {
                if (minTime != null)
                {
                    AddDateTimeToQueryParams(queryParams, "minFinishTime", minTime.Value);
                }
                if (maxTime != null)
                {
                    AddDateTimeToQueryParams(queryParams, "maxFinishTime", maxTime.Value);
                }
            }
            else
            {
                if (minTime != null)
                {
                    AddDateTimeToQueryParams(queryParams, "minTime", minTime.Value);
                }
                if (maxTime != null)
                {
                    AddDateTimeToQueryParams(queryParams, "maxTime", maxTime.Value);
                }
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
            if (!string.IsNullOrEmpty(repositoryId))
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (!string.IsNullOrEmpty(repositoryType))
            {
                queryParams.Add("repositoryType", repositoryType);
            }

            return queryParams;
        }

        private string NormalizeJsonPatchPath(string key)
        {
            const string JsonPatchPathStartString = "/";
            if (key.StartsWith(JsonPatchPathStartString))
            {
                return key;
            }

            return string.Format("{0}{1}", JsonPatchPathStartString, key);
        }

        private static readonly ApiResourceVersion s_ChangesApiVersion = new ApiResourceVersion("4.1-preview.2");
        private static readonly Guid s_getBuildsLocationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
    }
}
