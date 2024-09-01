using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.Results.Client;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Utilities.Internal;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(JobServer))]
    public interface IJobServer : IRunnerService, IAsyncDisposable
    {
        Task ConnectAsync(VssConnection jobConnection);

        void InitializeWebsocketClient(ServiceEndpoint serviceEndpoint);

        // logging and console
        Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken);
        Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken);
        Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, String type, String name, Stream uploadStream, CancellationToken cancellationToken);
        Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken);
        Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);
        Task RaisePlanEventAsync<T>(Guid scopeIdentifier, string hubName, Guid planId, T eventData, CancellationToken cancellationToken) where T : JobEvent;
        Task<Timeline> GetTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<ActionDownloadInfoCollection> ResolveActionDownloadInfoAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken);
    }

    public sealed class JobServer : RunnerService, IJobServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskHttpClient _taskClient;
        private ClientWebSocket _websocketClient;

        private ServiceEndpoint _serviceEndpoint;

        private int totalBatchedLinesAttemptedByWebsocket = 0;
        private int failedAttemptsToPostBatchedLinesByWebsocket = 0;


        private static readonly TimeSpan _minDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan _maxDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(500);
        private static readonly int _minWebsocketFailurePercentageAllowed = 50;
        private static readonly int _minWebsocketBatchedLinesCountToConsider = 5;

        private Task _websocketConnectTask;

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

        public void InitializeWebsocketClient(ServiceEndpoint serviceEndpoint)
        {
            this._serviceEndpoint = serviceEndpoint;
            InitializeWebsocketClient(TimeSpan.Zero);
        }

        public ValueTask DisposeAsync()
        {
            CloseWebSocket(WebSocketCloseStatus.NormalClosure, CancellationToken.None);

            GC.SuppressFinalize(this);

            return ValueTask.CompletedTask;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        private void InitializeWebsocketClient(TimeSpan delay)
        {
            if (_serviceEndpoint.Authorization != null &&
                _serviceEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out var accessToken) &&
                !string.IsNullOrEmpty(accessToken))
            {
                if (_serviceEndpoint.Data.TryGetValue("FeedStreamUrl", out var feedStreamUrl) && !string.IsNullOrEmpty(feedStreamUrl))
                {
                    // let's ensure we use the right scheme
                    feedStreamUrl = feedStreamUrl.Replace("https://", "wss://").Replace("http://", "ws://");
                    Trace.Info($"Creating websocket client ..." + feedStreamUrl);
                    this._websocketClient = new ClientWebSocket();
                    this._websocketClient.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                    var userAgentValues = new List<ProductInfoHeaderValue>();
                    userAgentValues.AddRange(UserAgentUtility.GetDefaultRestUserAgent());
                    userAgentValues.AddRange(HostContext.UserAgents);
                    this._websocketClient.Options.SetRequestHeader("User-Agent", string.Join(" ", userAgentValues.Select(x => x.ToString())));
                    if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_TLS_NO_VERIFY")))
                    {
                        this._websocketClient.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                    }

                    this._websocketConnectTask = ConnectWebSocketClient(feedStreamUrl, delay);
                }
                else
                {
                    Trace.Info($"No FeedStreamUrl found, so we will use Rest API calls for sending feed data");
                }
            }
            else
            {
                Trace.Info($"No access token from the service endpoint");
            }
        }

        private async Task ConnectWebSocketClient(string feedStreamUrl, TimeSpan delay)
        {
            try
            {
                Trace.Info($"Attempting to start websocket client with delay {delay}.");
                await Task.Delay(delay);
                using var connectTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await this._websocketClient.ConnectAsync(new Uri(feedStreamUrl), connectTimeoutTokenSource.Token);
                Trace.Info($"Successfully started websocket client.");
            }
            catch (Exception ex)
            {
                Trace.Info("Exception caught during websocket client connect, fallback of HTTP would be used now instead of websocket.");
                Trace.Error(ex);
                this._websocketClient = null;
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

        public async Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken)
        {
            CheckConnection();
            var pushedLinesViaWebsocket = false;
            if (_websocketConnectTask != null)
            {
                await _websocketConnectTask;
            }

            // "_websocketClient != null" implies either: We have a successful connection OR we have to attempt sending again and then reconnect
            // ...in other words, if websocket client is null, we will skip sending to websocket and just use rest api calls to send data
            if (_websocketClient != null)
            {
                var linesWrapper = startLine.HasValue ? new TimelineRecordFeedLinesWrapper(stepId, lines, startLine.Value) : new TimelineRecordFeedLinesWrapper(stepId, lines);
                var jsonData = StringUtil.ConvertToJson(linesWrapper);
                try
                {
                    totalBatchedLinesAttemptedByWebsocket++;
                    var jsonDataBytes = Encoding.UTF8.GetBytes(jsonData);
                    // break the message into chunks of 1024 bytes
                    for (var i = 0; i < jsonDataBytes.Length; i += 1 * 1024)
                    {
                        var lastChunk = i + (1 * 1024) >= jsonDataBytes.Length;
                        var chunk = new ArraySegment<byte>(jsonDataBytes, i, Math.Min(1 * 1024, jsonDataBytes.Length - i));
                        await _websocketClient.SendAsync(chunk, WebSocketMessageType.Text, endOfMessage: lastChunk, cancellationToken);
                    }

                    pushedLinesViaWebsocket = true;
                }
                catch (Exception ex)
                {
                    failedAttemptsToPostBatchedLinesByWebsocket++;
                    Trace.Info($"Caught exception during append web console line to websocket, let's fallback to sending via non-websocket call (total calls: {totalBatchedLinesAttemptedByWebsocket}, failed calls: {failedAttemptsToPostBatchedLinesByWebsocket}, websocket state: {this._websocketClient?.State}).");
                    Trace.Verbose(ex.ToString());
                    if (totalBatchedLinesAttemptedByWebsocket > _minWebsocketBatchedLinesCountToConsider)
                    {
                        // let's consider failure percentage
                        if (failedAttemptsToPostBatchedLinesByWebsocket * 100 / totalBatchedLinesAttemptedByWebsocket > _minWebsocketFailurePercentageAllowed)
                        {
                            Trace.Info($"Exhausted websocket allowed retries, we will not attempt websocket connection for this job to post lines again.");
                            CloseWebSocket(WebSocketCloseStatus.InternalServerError, cancellationToken);

                            // By setting it to null, we will ensure that we never try websocket path again for this job
                            _websocketClient = null;
                        }
                    }

                    if (_websocketClient != null)
                    {
                        var delay = BackoffTimerHelper.GetRandomBackoff(_minDelayForWebsocketReconnect, _maxDelayForWebsocketReconnect);
                        Trace.Info($"Websocket is not open, let's attempt to connect back again with random backoff {delay} ms (total calls: {totalBatchedLinesAttemptedByWebsocket}, failed calls: {failedAttemptsToPostBatchedLinesByWebsocket}).");
                        InitializeWebsocketClient(delay);
                    }
                }
            }

            if (!pushedLinesViaWebsocket && !cancellationToken.IsCancellationRequested)
            {
                if (startLine.HasValue)
                {
                    await _taskClient.AppendTimelineRecordFeedAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, stepId, lines, startLine.Value, cancellationToken: cancellationToken);
                }
                else
                {
                    await _taskClient.AppendTimelineRecordFeedAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, stepId, lines, cancellationToken: cancellationToken);
                }
            }
        }

        private void CloseWebSocket(WebSocketCloseStatus closeStatus, CancellationToken cancellationToken)
        {
            try
            {
                _websocketClient?.CloseOutputAsync(closeStatus, "Closing websocket", cancellationToken);
            }
            catch (Exception websocketEx)
            {
                // In some cases this might be okay since the websocket might be open yet, so just close and don't trace exceptions
                Trace.Info($"Failed to close websocket gracefully {websocketEx.GetType().Name}");
            }
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
        public Task<ActionDownloadInfoCollection> ResolveActionDownloadInfoAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid jobId, ActionReferenceList actions, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.ResolveActionDownloadInfoAsync(scopeIdentifier, hubName, planId, jobId, actions, cancellationToken: cancellationToken);
        }
    }
}
