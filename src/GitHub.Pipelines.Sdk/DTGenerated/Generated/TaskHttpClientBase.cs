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
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public abstract class TaskHttpClientBase : VssHttpClientBase
    {
        public TaskHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAttachment>> GetPlanAttachmentsAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            string type,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("eb55e5d6-2f30-4295-b5ed-38da50b1fc52");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, type = type };

            return SendAsync<List<TaskAttachment>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="recordId"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="uploadStream">Stream to upload</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAttachment> CreateAttachmentAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            string type,
            string name,
            Stream uploadStream,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("7898f959-9cdf-4096-b29e-7f293031629e");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId, type = type, name = name };
            HttpContent content = new StreamContent(uploadStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return SendAsync<TaskAttachment>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="recordId"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskAttachment> GetAttachmentAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            string type,
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7898f959-9cdf-4096-b29e-7f293031629e");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId, type = type, name = name };

            return SendAsync<TaskAttachment>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="recordId"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetAttachmentContentAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            string type,
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7898f959-9cdf-4096-b29e-7f293031629e");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId, type = type, name = name };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.1-preview.1"),
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
        /// [Preview API]
        /// </summary>
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="recordId"></param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskAttachment>> GetAttachmentsAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            string type,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7898f959-9cdf-4096-b29e-7f293031629e");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId, type = type };

            return SendAsync<List<TaskAttachment>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="recordId"></param>
        /// <param name="lines"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task AppendTimelineRecordFeedAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            TimelineRecordFeedLinesWrapper lines,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("858983e4-19bd-4c5e-864c-507b59b58b12");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId };
            HttpContent content = new ObjectContent<TimelineRecordFeedLinesWrapper>(lines, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="orchestrationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskAgentJob> GetJobInstanceAsync(
            Guid scopeIdentifier,
            string hubName,
            string orchestrationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0a1efd25-abda-43bd-9629-6c7bdd2e0d60");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, orchestrationId = orchestrationId };

            return SendAsync<TaskAgentJob>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="logId"></param>
        /// <param name="uploadStream">Stream to upload</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskLog> AppendLogContentAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            int logId,
            Stream uploadStream,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("46f5667d-263a-4684-91b1-dff7fdcf64e2");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, logId = logId };
            HttpContent content = new StreamContent(uploadStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return SendAsync<TaskLog>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="log"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<TaskLog> CreateLogAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            TaskLog log,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("46f5667d-263a-4684-91b1-dff7fdcf64e2");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };
            HttpContent content = new ObjectContent<TaskLog>(log, new VssJsonMediaTypeFormatter(true));

            return SendAsync<TaskLog>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="logId"></param>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetLogAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("46f5667d-263a-4684-91b1-dff7fdcf64e2");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
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
        /// [Preview API]
        /// </summary>
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TaskLog>> GetLogsAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("46f5667d-263a-4684-91b1-dff7fdcf64e2");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };

            return SendAsync<List<TaskLog>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskOrchestrationPlanGroupsQueueMetrics>> GetPlanGroupsQueueMetricsAsync(
            Guid scopeIdentifier,
            string hubName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("038fd4d5-cda7-44ca-92c0-935843fee1a7");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName };

            return SendAsync<List<TaskOrchestrationPlanGroupsQueueMetrics>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="statusFilter"></param>
        /// <param name="count"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<List<TaskOrchestrationQueuedPlanGroup>> GetQueuedPlanGroupsAsync(
            Guid scopeIdentifier,
            string hubName,
            PlanGroupStatus? statusFilter = null,
            int? count = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0dd73091-3e36-4f43-b443-1b76dd426d84");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (count != null)
            {
                queryParams.Add("count", count.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TaskOrchestrationQueuedPlanGroup>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planGroup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskOrchestrationQueuedPlanGroup> GetQueuedPlanGroupAsync(
            Guid scopeIdentifier,
            string hubName,
            string planGroup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("65fd0708-bc1e-447b-a731-0587c5464e5b");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planGroup = planGroup };

            return SendAsync<TaskOrchestrationQueuedPlanGroup>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<TaskOrchestrationPlan> GetPlanAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("5cecd946-d704-471e-a45f-3b4064fcfaba");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };

            return SendAsync<TaskOrchestrationPlan>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="changeId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TimelineRecord>> GetRecordsAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            int? changeId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8893bc5b-35b2-4be7-83cb-99e683551db4");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (changeId != null)
            {
                queryParams.Add("changeId", changeId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<TimelineRecord>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="records"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<TimelineRecord>> UpdateRecordsAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            VssJsonCollectionWrapper<IEnumerable<TimelineRecord>> records,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("8893bc5b-35b2-4be7-83cb-99e683551db4");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId };
            HttpContent content = new ObjectContent<VssJsonCollectionWrapper<IEnumerable<TimelineRecord>>>(records, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<TimelineRecord>>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timeline"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Timeline> CreateTimelineAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Timeline timeline,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("83597576-cc2c-453c-bea6-2882ae6a1653");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };
            HttpContent content = new ObjectContent<Timeline>(timeline, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Timeline>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTimelineAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("83597576-cc2c-453c-bea6-2882ae6a1653");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId };

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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="timelineId"></param>
        /// <param name="changeId"></param>
        /// <param name="includeRecords"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Timeline> GetTimelineAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            int? changeId = null,
            bool? includeRecords = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("83597576-cc2c-453c-bea6-2882ae6a1653");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (changeId != null)
            {
                queryParams.Add("changeId", changeId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (includeRecords != null)
            {
                queryParams.Add("includeRecords", includeRecords.Value.ToString());
            }

            return SendAsync<Timeline>(
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
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Timeline>> GetTimelinesAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("83597576-cc2c-453c-bea6-2882ae6a1653");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };

            return SendAsync<List<Timeline>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
