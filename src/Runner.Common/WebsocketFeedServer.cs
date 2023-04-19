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
using GitHub.Services.WebApi.Utilities.Internal;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(WebsocketFeedServer))]
    public interface IWebsocketFeedServer : IRunnerService, IAsyncDisposable
    {
        void InitializeWebsocketClient(ServiceEndpoint serviceEndpoint);
        Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken);
    }

    public sealed class WebsocketFeedServer : RunnerService, IWebsocketFeedServer
    {
        private ClientWebSocket _websocketClient;

        private int totalBatchedLinesAttemptedByWebsocket = 0;
        private int failedAttemptsToPostBatchedLinesByWebsocket = 0;

        private static readonly TimeSpan _minDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan _maxDelayForWebsocketReconnect = TimeSpan.FromMilliseconds(500);
        private static readonly int _minWebsocketFailurePercentageAllowed = 50;
        private static readonly int _minWebsocketBatchedLinesCountToConsider = 5;

        private Task _websocketConnectTask;

        private ServiceEndpoint _serviceEndpoint;

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
                await this._websocketClient.ConnectAsync(new Uri(feedStreamUrl), default(CancellationToken));
                Trace.Info($"Successfully started websocket client.");
            }
            catch (Exception ex)
            {
                Trace.Info("Exception caught during websocket client connect, fallback of HTTP would be used now instead of websocket.");
                Trace.Error(ex);
            }
        }

        public async Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long? startLine, CancellationToken cancellationToken)
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
                    totalBatchedLinesAttemptedByWebsocket++;
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
                    failedAttemptsToPostBatchedLinesByWebsocket++;
                    Trace.Info($"Caught exception during append web console line to websocket, let's fallback to sending via non-websocket call (total calls: {totalBatchedLinesAttemptedByWebsocket}, failed calls: {failedAttemptsToPostBatchedLinesByWebsocket}, websocket state: {this._websocketClient?.State}).");
                    Trace.Error(ex);
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
