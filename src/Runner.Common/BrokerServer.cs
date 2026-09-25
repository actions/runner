using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Sdk.RSWebApi.Contracts;
using Sdk.WebApi.WebApi.RawClient;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteSessionAsync(CancellationToken cancellationToken);

        Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken token);

        Task AcknowledgeRunnerRequestAsync(string runnerRequestId, Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, CancellationToken token);

        Task UpdateConnectionIfNeeded(Uri serverUri, VssCredentials credentials);

        Task ForceRefreshConnection(VssCredentials credentials);

        // Temporary probe: holds/reconnects a websocket to the broker listener until cancelled.
        Task<BrokerWebSocketProbeResult> RunLongPollWebSocketProbeAsync(CancellationToken cancellationToken);
    }

    public sealed class BrokerServer : RunnerService, IBrokerServer
    {
        private bool _hasConnection;
        private Uri _brokerUri;
        private RawConnection _connection;
        private BrokerHttpClient _brokerHttpClient;

        private static readonly TimeSpan MinDelayForWebSocketReconnect = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MaxDelayForWebSocketReconnect = TimeSpan.FromSeconds(300);

        public async Task ConnectAsync(Uri serverUri, VssCredentials credentials)
        {
            Trace.Entering();
            _brokerUri = serverUri;

            _connection = VssUtil.CreateRawConnection(serverUri, credentials);
            _brokerHttpClient = await _connection.GetClientAsync<BrokerHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public async Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection();
            var jobMessage = await _brokerHttpClient.CreateSessionAsync(session, cancellationToken);

            return jobMessage;
        }

        public Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken)
        {
            CheckConnection();
            var brokerSession = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(sessionId, version, status, os, architecture, disableUpdate, cancellationToken), cancellationToken, shouldRetry: ShouldRetryException);

            return brokerSession;
        }

        public async Task AcknowledgeRunnerRequestAsync(string runnerRequestId, Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, CancellationToken cancellationToken)
        {
            CheckConnection();

            // No retries
            await _brokerHttpClient.AcknowledgeRunnerRequestAsync(runnerRequestId, sessionId, version, status, os, architecture, cancellationToken);
        }

        public async Task<BrokerWebSocketProbeResult> RunLongPollWebSocketProbeAsync(CancellationToken cancellationToken)
        {
            CheckConnection();

            var result = new BrokerWebSocketProbeResult();
            var stopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = await ConnectWebSocketAsync(cancellationToken);
                if (socket == null)
                {
                    result.ConnectFailures++;
                    result.LastCloseReason = "connect_failed";

                    var delay = BackoffTimerHelper.GetRandomBackoff(MinDelayForWebSocketReconnect, MaxDelayForWebSocketReconnect);
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    continue;
                }

                result.Connected = true;
                result.ConnectCount++;

                using (socket)
                {
                    var buffer = new byte[4096];
                    try
                    {
                        while (socket.State == WebSocketState.Open)
                        {
                            var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            if (receiveResult.MessageType == WebSocketMessageType.Close)
                            {
                                result.LastCloseReason = $"server_closed:{receiveResult.CloseStatus}:{receiveResult.CloseStatusDescription}";
                                Trace.Info($"Runner long-poll websocket closed by server. CloseStatus: {receiveResult.CloseStatus}, Description: {receiveResult.CloseStatusDescription}");
                                await socket.CloseOutputAsync(receiveResult.CloseStatus.Value, "Closing websocket", cancellationToken);
                                break;
                            }

                            result.PingsReceived++;
                            Trace.Info($"Runner long-poll websocket received a ping: " + $"{Encoding.UTF8.GetString(buffer, 0, receiveResult.Count)}");
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        result.LastCloseReason = "job_completed";
                    }
                    catch (Exception ex)
                    {
                        Trace.Info("Exception caught while holding runner long-poll websocket, will reconnect.");
                        Trace.Error(ex);
                        result.Errors.Add(ex.Message);
                        result.LastCloseReason = "error";
                    }

                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing websocket", CancellationToken.None);
                    }
                }
            }

            stopwatch.Stop();
            result.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private async Task<ClientWebSocket> ConnectWebSocketAsync(CancellationToken cancellationToken)
        {
            try
            {
                Trace.Info("Attempting to start runner long-poll websocket client.");
                var socket = await _brokerHttpClient.ConnectRunnerLongPollWebSocketAsync(cancellationToken);
                Trace.Info("Successfully started runner long-poll websocket client.");
                return socket;
            }
            catch (Exception ex)
            {
                Trace.Info("Exception caught during runner long-poll websocket connect, will retry.");
                Trace.Error(ex);
                return null;
            }
        }

        // Best-effort close; the socket may already be closed/faulted.
        private void CloseWebSocket(ClientWebSocket socket, WebSocketCloseStatus closeStatus, CancellationToken cancellationToken)
        {
            try
            {
                socket?.CloseOutputAsync(closeStatus, "Closing websocket", cancellationToken);
            }
            catch (Exception websocketEx)
            {
                Trace.Info($"Failed to close websocket gracefully {websocketEx.GetType().Name}");
            }
        }

        public async Task DeleteSessionAsync(CancellationToken cancellationToken)
        {
            CheckConnection();
            await _brokerHttpClient.DeleteSessionAsync(cancellationToken);
        }

        public Task UpdateConnectionIfNeeded(Uri serverUri, VssCredentials credentials)
        {
            if (_brokerUri != serverUri || !_hasConnection)
            {
                return ConnectAsync(serverUri, credentials);
            }

            return Task.CompletedTask;
        }

        public Task ForceRefreshConnection(VssCredentials credentials)
        {
            if (!string.IsNullOrEmpty(_brokerUri?.AbsoluteUri))
            {
                return ConnectAsync(_brokerUri, credentials);
            }

            return Task.CompletedTask;
        }

        public bool ShouldRetryException(Exception ex)
        {
            if (ex is AccessDeniedException || ex is VssUnauthorizedException || ex is RunnerNotFoundException || ex is HostedRunnerDeprovisionedException || ex is TaskAgentSessionExpiredException)
            {
                return false;
            }

            return true;
        }
    }
}
