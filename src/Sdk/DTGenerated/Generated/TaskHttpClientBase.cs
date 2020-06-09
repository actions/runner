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
        /// [Preview API] Resolves information required to download actions (URL, token) defined in an orchestration.
        /// </summary>
        /// <param name="scopeIdentifier">The project GUID to scope the request</param>
        /// <param name="hubName">The name of the server hub: "build" for the Build server or "rm" for the Release Management server</param>
        /// <param name="planId"></param>
        /// <param name="actionReferenceList"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<ActionDownloadInfoCollection> ResolveActionDownloadInfoAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            ActionReferenceList actionReferenceList,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("27d7f831-88c1-4719-8ca1-6a061dad90eb");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId };
            HttpContent content = new ObjectContent<ActionReferenceList>(actionReferenceList, new VssJsonMediaTypeFormatter(true));

            return SendAsync<ActionDownloadInfoCollection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
