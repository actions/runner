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
            IEnumerable<Demand> demands = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            IEnumerable<String> demandStrings = null;
            if (demands != null)
            {
                demandStrings = demands.Select(d => d.ToString());
            }
            return GetAgentsAsync(poolId, agentName, includeCapabilities, includeAssignedRequest, includeLastCompletedRequest, propertyFilters, demandStrings, userState, cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a secure file
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileId">The unique secure file Id</param>
        /// <param name="includeDownloadTicket">If includeDownloadTicket is true and the caller has permissions, a download ticket is included in the response.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<SecureFile> GetSecureFileAsync(
            Guid project,
            Guid secureFileId,
            bool? includeDownloadTicket = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSecureFileAsync(project, secureFileId, includeDownloadTicket, actionFilter: null, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SecureFile>> GetSecureFilesAsync(
            string project,
            string namePattern = null,
            bool? includeDownloadTickets = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSecureFilesAsync(project, namePattern, includeDownloadTickets, actionFilter: null, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="namePattern">Name of the secure file to match. Can include wildcards to match multiple files.</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SecureFile>> GetSecureFilesAsync(
            Guid project,
            string namePattern = null,
            bool? includeDownloadTickets = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSecureFilesAsync(project, namePattern, includeDownloadTickets, actionFilter: null, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="secureFileIds">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SecureFile>> GetSecureFilesByIdsAsync(
            string project,
            IEnumerable<Guid> secureFileIds,
            bool? includeDownloadTickets = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSecureFilesByIdsAsync(project, secureFileIds, includeDownloadTickets, actionFilter: null, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get secure files
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="secureFileIds">A list of secure file Ids</param>
        /// <param name="includeDownloadTickets">If includeDownloadTickets is true and the caller has permissions, a download ticket for each secure file is included in the response.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SecureFile>> GetSecureFilesByIdsAsync(
            Guid project,
            IEnumerable<Guid> secureFileIds,
            bool? includeDownloadTickets = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSecureFilesByIdsAsync(project, secureFileIds, includeDownloadTickets, actionFilter: null, userState: userState, cancellationToken: cancellationToken);
        }

        public async Task<Stream> GetTaskContentZipAsync(
            Guid taskId,
            TaskVersion version,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var routeValues = new { taskId = taskId, versionString = version.ToString() };
            HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                HttpMethod.Get,
                TaskResourceIds.Tasks,
                routeValues: routeValues,
                version: m_currentApiVersion).ConfigureAwait(false);

            requestMessage.Headers.Accept.Clear();
            var header = new MediaTypeWithQualityHeaderValue("application/zip");
            header.Parameters.Add(new NameValueHeaderValue("api-version", m_currentApiVersion.ApiVersionString));
            header.Parameters.Add(new NameValueHeaderValue("res-version", "1"));
            requestMessage.Headers.Accept.Add(header);

            HttpResponseMessage response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                throw new Exception("no content");
            }

            if (!VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "application/zip"))
            {
                throw new Exception("bad content type");
            }

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                return new GZipStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), CompressionMode.Decompress);
            }

            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public Task<TaskAgentJobRequest> QueueAgentRequestByPoolAsync(
            Int32 poolId,
            IList<Demand> demands,
            Guid serviceOwner,
            Guid hostId,
            Guid scopeIdentifier,
            String hubName,
            Guid planId,
            Guid jobId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new TaskAgentJobRequest
            {
                ServiceOwner = serviceOwner,
                HostId = hostId,
                PlanType = hubName,
                ScopeId = scopeIdentifier,
                PlanId = planId,
                JobId = jobId
            };

            return QueueAgentRequestByPoolAsync(poolId, request, userState, cancellationToken);
        }

        public Task<TaskAgentJobRequest> RenewAgentRequestAsync(
            Int32 poolId,
            Int64 requestId,
            Guid lockToken,
            DateTime? expiresOn = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new TaskAgentJobRequest
            {
                RequestId = requestId,
                LockedUntil = expiresOn,
            };

            return UpdateAgentRequestAsync(poolId, requestId, lockToken, request, userState, cancellationToken);
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

        public Task SendMessageAsync(
            Int32 poolId,
            Int64 requestId,
            AgentJobRequestMessage request,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new TaskAgentMessage
            {
                Body = JsonUtility.ToString(request),
                MessageType = request.MessageType,
            };

            return SendMessageAsync(poolId, requestId, message, userState: userState, cancellationToken: cancellationToken);
        }

        public Task SendMessageAsync(
            Int32 poolId,
            Int64 requestId,
            JobCancelMessage cancel,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new TaskAgentMessage
            {
                Body = JsonUtility.ToString(cancel),
                MessageType = JobCancelMessage.MessageType,
            };

            return SendMessageAsync(poolId, requestId, message, userState: userState, cancellationToken: cancellationToken);
        }

        public async Task<HttpResponseMessage> UploadTaskZipAsync(
            Guid taskId,
            Stream fileStream,
            Boolean overwrite = false,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(fileStream, "fileStream");

            HttpRequestMessage requestMessage;

            if (fileStream.Length == 0)
            {
                throw new Exception("file stream of length 0 not allowed.");
            }

            if (fileStream.Length > 16 * 1024 * 1024)
            {
                throw new Exception("file stream too big");
            }

            Byte[] dataToSend = new Byte[fileStream.Length];

            List<KeyValuePair<String, String>> queryParameters = null;
            if (overwrite)
            {
                queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("overwrite", "true");
            }

            var routeValues = new
            {
                taskId = taskId
            };

            requestMessage = await CreateRequestMessageAsync(HttpMethod.Put,
                                                             TaskResourceIds.Tasks,
                                                             routeValues: routeValues,
                                                             version: m_currentApiVersion,
                                                             queryParameters: queryParameters,
                                                             userState: userState,
                                                             cancellationToken: cancellationToken).ConfigureAwait(false);

            // inorder for the upload to be retryable, we need the content to be re-readable
            // to ensure this we copy the chunk into a byte array and send that
            // chunk size ensures we can convert the length to an int
            int bytesToCopy = (int)fileStream.Length;
            using (MemoryStream ms = new MemoryStream(dataToSend))
            {
                await fileStream.CopyToAsync(ms, bytesToCopy, cancellationToken).ConfigureAwait(false);
            }

            // set the content and the Content-Range header
            HttpContent byteArrayContent = new ByteArrayContent(dataToSend, 0, bytesToCopy);
            byteArrayContent.Headers.ContentLength = fileStream.Length;
            byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileStream.Length - 1, fileStream.Length);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            requestMessage.Content = byteArrayContent;
            return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupName"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<DeploymentGroupMetrics>> GetDeploymentGroupsMetricsAsync2(
            string project,
            string deploymentGroupName = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("281c6308-427a-49e1-b83a-dac0f4862189");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(deploymentGroupName))
            {
                queryParams.Add("deploymentGroupName", deploymentGroupName);
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<IPagedList<DeploymentGroupMetrics>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<DeploymentGroupMetrics>);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupName"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<IPagedList<DeploymentGroupMetrics>> GetDeploymentGroupsMetricsAsync2(
            Guid project,
            string deploymentGroupName = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("281c6308-427a-49e1-b83a-dac0f4862189");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(deploymentGroupName))
            {
                queryParams.Add("deploymentGroupName", deploymentGroupName);
            }
            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<IPagedList<DeploymentGroupMetrics>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<DeploymentGroupMetrics>);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="tags"></param>
        /// <param name="name"></param>
        /// <param name="partialNameMatch"></param>
        /// <param name="expand"></param>
        /// <param name="agentStatus"></param>
        /// <param name="agentJobResult"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="propertyFilters"></param>
        public virtual Task<IPagedList<DeploymentMachine>> GetDeploymentTargetsAsyncWithContinuationToken(
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
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Boolean? enabled = null,
            IEnumerable<string> propertyFilters = null)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (!string.IsNullOrEmpty(name))
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
            if (!string.IsNullOrEmpty(continuationToken))
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

            return SendAsync<IPagedList<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.1-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<DeploymentMachine>);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="deploymentGroupId"></param>
        /// <param name="tags"></param>
        /// <param name="name"></param>
        /// <param name="partialNameMatch"></param>
        /// <param name="expand"></param>
        /// <param name="agentStatus"></param>
        /// <param name="agentJobResult"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="propertyFilters"></param>
        public virtual Task<IPagedList<DeploymentMachine>> GetDeploymentTargetsAsyncWithContinuationToken(
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
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Boolean? enabled = null,
            IEnumerable<string> propertyFilters = null)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2f0aa599-c121-4256-a5fd-ba370e0ae7b6");
            object routeValues = new { project = project, deploymentGroupId = deploymentGroupId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tags != null && tags.Any())
            {
                queryParams.Add("tags", string.Join(",", tags));
            }
            if (!string.IsNullOrEmpty(name))
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
            if (!string.IsNullOrEmpty(continuationToken))
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

            return SendAsync<IPagedList<DeploymentMachine>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.1-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                processResponse: GetPagedList<DeploymentMachine>);
        }

        protected async Task<IPagedList<T>> GetPagedList<T>(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            var continuationToken = GetContinuationToken(responseMessage);
            var list = await ReadContentAsAsync<List<T>>(responseMessage, cancellationToken).ConfigureAwait(false);
            return new PagedList<T>(list, continuationToken);
        }

        protected string GetContinuationToken(HttpResponseMessage responseMessage)
        {
            string continuationToken = null;

            IEnumerable<string> headerValues = null;
            if (responseMessage.Headers.TryGetValues("x-ms-continuationtoken", out headerValues))
            {
                continuationToken = headerValues.FirstOrDefault();
            }

            return continuationToken;
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
