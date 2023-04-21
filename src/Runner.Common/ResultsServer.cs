using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.Results.Client;
using GitHub.Services.WebApi.Utilities.Internal;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(ResultServer))]
    public interface IResultsServer : IRunnerService, IAsyncDisposable
    {
        void InitializeResultsClient(Uri uri, string liveConsoleFeedUrl, string token);

        Task AppendLiveConsoleFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken);

        // logging and console
        Task CreateResultsStepSummaryAsync(string planId, string jobId, Guid stepId, string file,
            CancellationToken cancellationToken);

        Task CreateResultsStepLogAsync(string planId, string jobId, Guid stepId, string file, bool finalize,
            bool firstBlock, long lineCount, CancellationToken cancellationToken);

        Task CreateResultsJobLogAsync(string planId, string jobId, string file, bool finalize, bool firstBlock,
            long lineCount, CancellationToken cancellationToken);

        Task UpdateResultsWorkflowStepsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId,
            IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);
    }

    public sealed class ResultServer : RunnerService, IResultsServer
    {
        private ResultsHttpClient _resultsClient;

        private ClientWebSocket _websocketClient;

        private int _totalBatchedLinesAttemptedByWebsocket = 0;
        private int _failedAttemptsToPostBatchedLinesByWebsocket = 0;

        private static readonly TimeSpan MinDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan MaxDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(500);
        private static readonly int MinWebsocketFailurePercentageAllowed = 50;
        private static readonly int MinWebsocketBatchedLinesCountToConsider = 5;

        private Task _websocketConnectTask;
        private String _liveConsoleFeedUrl;
        private string _token;

        public void InitializeResultsClient(Uri uri, string liveConsoleFeedUrl, string token)
        {
            var httpMessageHandler = HostContext.CreateHttpClientHandler();
            this._resultsClient = new ResultsHttpClient(uri, httpMessageHandler, token, disposeHandler: true);
            _token = token;
            if (!string.IsNullOrEmpty(liveConsoleFeedUrl))
            {
                _liveConsoleFeedUrl = liveConsoleFeedUrl;
                InitializeWebsocketClient(liveConsoleFeedUrl, token, TimeSpan.Zero);
            }
        }

        public Task CreateResultsStepSummaryAsync(string planId, string jobId, Guid stepId, string file,
            CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadStepSummaryAsync(planId, jobId, stepId, file,
                    cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task CreateResultsStepLogAsync(string planId, string jobId, Guid stepId, string file, bool finalize,
            bool firstBlock, long lineCount, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadResultsStepLogAsync(planId, jobId, stepId, file, finalize, firstBlock,
                    lineCount, cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task CreateResultsJobLogAsync(string planId, string jobId, string file, bool finalize, bool firstBlock,
            long lineCount, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadResultsJobLogAsync(planId, jobId, file, finalize, firstBlock, lineCount,
                    cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task UpdateResultsWorkflowStepsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId,
            IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                try
                {
                    var timelineRecords = records.ToList();
                    return _resultsClient.UpdateWorkflowStepsAsync(planId, new List<TimelineRecord>(timelineRecords),
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log error, but continue as this call is best-effort
                    Trace.Info($"Failed to update steps status due to {ex.GetType().Name}");
                    Trace.Error(ex);
                }
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public ValueTask DisposeAsync()
        {
            CloseWebSocket(WebSocketCloseStatus.NormalClosure, CancellationToken.None);

            GC.SuppressFinalize(this);

            return ValueTask.CompletedTask;
        }

        private void InitializeWebsocketClient(string liveConsoleFeedUrl, string accessToken, TimeSpan delay)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                if (!string.IsNullOrEmpty(liveConsoleFeedUrl))
                {
                    // let's ensure we use the right scheme
                    liveConsoleFeedUrl = liveConsoleFeedUrl.Replace("https://", "wss://").Replace("http://", "ws://");
                    Trace.Info($"Creating websocket client ..." + liveConsoleFeedUrl);
                    this._websocketClient = new ClientWebSocket();
                    this._websocketClient.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                    var userAgentValues = new List<ProductInfoHeaderValue>();
                    userAgentValues.AddRange(UserAgentUtility.GetDefaultRestUserAgent());
                    userAgentValues.AddRange(HostContext.UserAgents);
                    this._websocketClient.Options.SetRequestHeader("User-Agent", string.Join(" ", userAgentValues.Select(x => x.ToString())));

                    this._websocketConnectTask = ConnectWebSocketClient(liveConsoleFeedUrl, delay);
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

        public async Task AppendLiveConsoleFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken)
        {
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
                    _totalBatchedLinesAttemptedByWebsocket++;
                    var jsonDataBytes = Encoding.UTF8.GetBytes(jsonData);
                    // break the message into chunks of 1024 bytes
                    for (var i = 0; i < jsonDataBytes.Length; i += 1 * 1024)
                    {
                        var lastChunk = i + (1 * 1024) >= jsonDataBytes.Length;
                        var chunk = new ArraySegment<byte>(jsonDataBytes, i, Math.Min(1 * 1024, jsonDataBytes.Length - i));
                        await _websocketClient.SendAsync(chunk, WebSocketMessageType.Text, endOfMessage: lastChunk, cancellationToken);
                    }

                }
                catch (Exception ex)
                {
                    _failedAttemptsToPostBatchedLinesByWebsocket++;
                    Trace.Info($"Caught exception during append web console line to websocket, let's fallback to sending via non-websocket call (total calls: {_totalBatchedLinesAttemptedByWebsocket}, failed calls: {_failedAttemptsToPostBatchedLinesByWebsocket}, websocket state: {this._websocketClient?.State}).");
                    Trace.Error(ex);
                    if (_totalBatchedLinesAttemptedByWebsocket > MinWebsocketBatchedLinesCountToConsider)
                    {
                        // let's consider failure percentage
                        if (_failedAttemptsToPostBatchedLinesByWebsocket * 100 / _totalBatchedLinesAttemptedByWebsocket > MinWebsocketFailurePercentageAllowed)
                        {
                            Trace.Info($"Exhausted websocket allowed retries, we will not attempt websocket connection for this job to post lines again.");
                            CloseWebSocket(WebSocketCloseStatus.InternalServerError, cancellationToken);

                            // By setting it to null, we will ensure that we never try websocket path again for this job
                            _websocketClient = null;
                        }
                    }

                    if (_websocketClient != null)
                    {
                        var delay = BackoffTimerHelper.GetRandomBackoff(MinDelayForWebsocketReconnect, MaxDelayForWebsocketReconnect);
                        Trace.Info($"Websocket is not open, let's attempt to connect back again with random backoff {delay} ms (total calls: {_totalBatchedLinesAttemptedByWebsocket}, failed calls: {_failedAttemptsToPostBatchedLinesByWebsocket}).");
                        InitializeWebsocketClient(_liveConsoleFeedUrl, _token, delay);
                    }
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
    }
}
