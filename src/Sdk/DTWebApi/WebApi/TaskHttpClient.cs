using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.DistributedTask.WebApi
{
    public sealed class TaskHttpClient : TaskHttpClientBase
    {
        public TaskHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
        
        public Task AppendTimelineRecordFeedAsync(
            Guid scopeIdentifier,
            String planType,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            IEnumerable<String> lines,
            CancellationToken cancellationToken = default(CancellationToken),
            Object userState = null)
        {
            return AppendTimelineRecordFeedAsync(scopeIdentifier,
                                                 planType,
                                                 planId,
                                                 timelineId,
                                                 recordId,
                                                 new TimelineRecordFeedLinesWrapper(Guid.Empty, lines.ToList()),
                                                 userState,
                                                 cancellationToken);
        }

        public Task AppendTimelineRecordFeedAsync(
            Guid scopeIdentifier,
            String planType,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            Guid stepId,
            IList<String> lines,
            CancellationToken cancellationToken = default(CancellationToken),
            Object userState = null)
        {
            return AppendTimelineRecordFeedAsync(scopeIdentifier,
                                                 planType,
                                                 planId,
                                                 timelineId,
                                                 recordId,
                                                 new TimelineRecordFeedLinesWrapper(stepId, lines),
                                                 userState,
                                                 cancellationToken);
        }
        
        public Task AppendTimelineRecordFeedAsync(
            Guid scopeIdentifier,
            String planType,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            Guid stepId,
            IList<String> lines,
            long startLine,
            CancellationToken cancellationToken = default(CancellationToken),
            Object userState = null)
        {
            return AppendTimelineRecordFeedAsync(scopeIdentifier,
                                                 planType,
                                                 planId,
                                                 timelineId,
                                                 recordId,
                                                 new TimelineRecordFeedLinesWrapper(stepId, lines, startLine),
                                                 userState,
                                                 cancellationToken);
        }

        public async Task RaisePlanEventAsync<T>(
            Guid scopeIdentifier,
            String planType,
            Guid planId,
            T eventData,
            CancellationToken cancellationToken = default(CancellationToken),
            Object userState = null) where T : JobEvent
        {
            var routeValues = new { scopeIdentifier = scopeIdentifier, hubName = planType, planId = planId };
            await base.PostAsync<T>(eventData,
                                    TaskResourceIds.PlanEvents,
                                    routeValues,
                                    version: m_currentApiVersion,
                                    cancellationToken: cancellationToken,
                                    userState: userState).ConfigureAwait(false);
        }

        public Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(
            Guid scopeIdentifier,
            String planType,
            Guid planId,
            Guid timelineId,
            IEnumerable<TimelineRecord> records,
            CancellationToken cancellationToken = default(CancellationToken),
            Object userState = null)
        {
            return UpdateRecordsAsync(scopeIdentifier,
                                      planType,
                                      planId,
                                      timelineId,
                                      new VssJsonCollectionWrapper<IEnumerable<TimelineRecord>>(records),
                                      userState,
                                      cancellationToken);
        }

        private readonly ApiResourceVersion m_currentApiVersion = new ApiResourceVersion(2.0, 1);
    }
}
