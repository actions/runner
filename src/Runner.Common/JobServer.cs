using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(JobServer))]
    public interface IJobServer : IRunnerService
    {
        Task ConnectAsync(VssConnection jobConnection);

        // logging and console
        Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken);
        Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long startLine, CancellationToken cancellationToken);
        StreamingFeedRequest CreateStreamingFeedRequest(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, TimeSpan maxRequestDuration, CancellationToken cancellationToken);
        Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, String type, String name, Stream uploadStream, CancellationToken cancellationToken);
        Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken);
        Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);
        Task RaisePlanEventAsync<T>(Guid scopeIdentifier, string hubName, Guid planId, T eventData, CancellationToken cancellationToken) where T : JobEvent;
        Task<Timeline> GetTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<ActionDownloadInfoCollection> ResolveActionDownloadInfoAsync(Guid scopeIdentifier, string hubName, Guid planId, ActionReferenceList actions, CancellationToken cancellationToken);
    }

    public sealed class JobServer : RunnerService, IJobServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskHttpClient _taskClient;
        private StreamingHttpClient _streamingClient;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;
            int totalAttempts = 5;
            int attemptCount = totalAttempts;
            var configurationStore = HostContext.GetService<IConfigurationStore>();
            var runnerSettings = configurationStore.GetSettings();
 
            while (!_connection.HasAuthenticated && attemptCount-- > 0)
            {
                try
                {
                    await _connection.ConnectAsync();
                    break;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attempts left.");
                    Trace.Error(ex);

                    if (runnerSettings.IsHostedServer)
                    {
                        await CheckNetworkEndpointsAsync(attemptCount);
                    }
                }

                int attempt = totalAttempts - attemptCount;
                TimeSpan backoff = BackoffTimerHelper.GetExponentialBackoff(attempt, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(3.2), TimeSpan.FromMilliseconds(100));

                await Task.Delay(backoff);
            }

            _taskClient = _connection.GetClient<TaskHttpClient>();
            _streamingClient = _connection.GetClient<StreamingHttpClient>();
            _hasConnection = true;
        }

        private async Task CheckNetworkEndpointsAsync(int attemptsLeft)
        {
            try
            {
                Trace.Info("Requesting Actions Service health endpoint status");
                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                using (var actionsClient = new HttpClient(httpClientHandler))
                {
                    var baseUri = new Uri(_connection.Uri.GetLeftPart(UriPartial.Authority));

                    actionsClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);

                    // Call the _apis/health endpoint, and include how many attempts are left as a URL query for easy tracking
                    var response = await actionsClient.GetAsync(new Uri(baseUri, $"_apis/health?_internalRunnerAttemptsLeft={attemptsLeft}"));
                    Trace.Info($"Actions health status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log error, but continue as this call is best-effort
                Trace.Info($"Actions Service health endpoint failed due to {ex.GetType().Name}");
                Trace.Error(ex);
            }

            try
            {
                Trace.Info("Requesting Github API endpoint status");
                // This is a dotcom public API... just call it directly
                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                using (var gitHubClient = new HttpClient(httpClientHandler))
                {
                    gitHubClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);

                    // Call the api.github.com endpoint, and include how many attempts are left as a URL query for easy tracking
                    var response = await gitHubClient.GetAsync($"https://api.github.com?_internalRunnerAttemptsLeft={attemptsLeft}");
                    Trace.Info($"api.github.com status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log error, but continue as this call is best-effort
                Trace.Info($"Github API endpoint failed due to {ex.GetType().Name}");
                Trace.Error(ex);
            }
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        //-----------------------------------------------------------------
        // Feedback: WebConsole, TimelineRecords and Logs
        //-----------------------------------------------------------------

        public Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.AppendLogContentAsync(scopeIdentifier, hubName, planId, logId, uploadStream, cancellationToken: cancellationToken);
        }

        public Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long startLine, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.AppendTimelineRecordFeedAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, stepId, lines, startLine, cancellationToken: cancellationToken);
        }

        public StreamingFeedRequest CreateStreamingFeedRequest(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, TimeSpan maxRequestTime, CancellationToken cancellationToken)
        {
            Trace.Info("Creating new streaming feed request");
            return new StreamingFeedRequest(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, _streamingClient, maxRequestTime, Trace);
        }

        public Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, string type, string name, Stream uploadStream, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateAttachmentAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, type, name, uploadStream, cancellationToken: cancellationToken);
        }

        public Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateLogAsync(scopeIdentifier, hubName, planId, log, cancellationToken: cancellationToken);
        }

        public Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateTimelineAsync(scopeIdentifier, hubName, planId, new Timeline(timelineId), cancellationToken: cancellationToken);
        }

        public Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.UpdateTimelineRecordsAsync(scopeIdentifier, hubName, planId, timelineId, records, cancellationToken: cancellationToken);
        }

        public Task RaisePlanEventAsync<T>(Guid scopeIdentifier, string hubName, Guid planId, T eventData, CancellationToken cancellationToken) where T : JobEvent
        {
            CheckConnection();
            return _taskClient.RaisePlanEventAsync(scopeIdentifier, hubName, planId, eventData, cancellationToken: cancellationToken);
        }

        public Task<Timeline> GetTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.GetTimelineAsync(scopeIdentifier, hubName, planId, timelineId, includeRecords: true, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // Action download info
        //-----------------------------------------------------------------
        public Task<ActionDownloadInfoCollection> ResolveActionDownloadInfoAsync(Guid scopeIdentifier, string hubName, Guid planId, ActionReferenceList actions, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.ResolveActionDownloadInfoAsync(scopeIdentifier, hubName, planId, actions, cancellationToken: cancellationToken);
        }
    }

    public class StreamingHttpClient : TaskHttpClientBase
    {
        public StreamingHttpClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }

        public StreamingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {
        }

        public StreamingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)
        {
        }

        public StreamingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public StreamingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)
        {
        }

        public virtual async Task StreamTimelineRecordFeedAsync(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid recordId,
            ChannelReader<TimelineRecordFeedLinesWrapper> eventReader,
            Tracing trace,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("858983e4-19bd-4c5e-864c-507b59b58b12");
            object routeValues = new { scopeIdentifier = scopeIdentifier, hubName = hubName, planId = planId, timelineId = timelineId, recordId = recordId };

            PushStreamContent pushContent = new PushStreamContent(async (stream, content, context) =>
            {
                using StreamWriter streamWriter = new StreamWriter(stream);
                using JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter);

                try
                {
                    JsonSerializer serializer = JsonUtility.CreateJsonSerializer();
                    await foreach (TimelineRecordFeedLinesWrapper lines in eventReader.ReadAllAsync(cancellationToken))
                    {
                        trace.Info($"Streaming timeline record feed: {lines.Value.Count} lines");
                        serializer.Serialize(streamWriter, lines);
                        await streamWriter.FlushAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    trace.Info("Streaming timeline record feed cancelled");
                }
                finally
                {
                    // always flush and close the stream
                    await streamWriter.FlushAsync().ConfigureAwait(false);
                    stream.Close();
                }
            }, "application/octet-stream");

            using (HttpRequestMessage request = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: CancellationToken.None, // we don't want to cancel the request
                content: pushContent,
                mediaType: "application/octet-stream").ConfigureAwait(false))
            {
                // in order to get proper streaming we need our client to not buffer the request
                request.Headers.TransferEncodingChunked = true;

                using (var response = await SendAsync(request, userState, CancellationToken.None).ConfigureAwait(false))
                {
                }
            }
        }
    }
}
