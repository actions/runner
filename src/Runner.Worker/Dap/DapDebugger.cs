using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using Microsoft.DevTunnels.Connections;
using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Stores information about a completed step for stack trace display.
    /// </summary>
    internal sealed class CompletedStepInfo
    {
        public string DisplayName { get; set; }
        public TaskResult? Result { get; set; }
        public int FrameId { get; set; }
    }

    /// <summary>
    /// Single public facade for the Debug Adapter Protocol subsystem.
    /// Owns the full transport, handshake, step-level pauses, variable
    /// inspection, reconnection, and cancellation flow.
    /// </summary>
    public sealed class DapDebugger : RunnerService, IDapDebugger
    {
        private const int _defaultTimeoutMinutes = 15;
        private const string _timeoutEnvironmentVariable = "ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT";
        private const int _defaultTunnelConnectTimeoutSeconds = 30;
        private const string _tunnelConnectTimeoutSeconds = "ACTIONS_RUNNER_DAP_TUNNEL_CONNECT_TIMEOUT_SECONDS";
        private const string _contentLengthHeader = "Content-Length: ";
        private const int _maxMessageSize = 10 * 1024 * 1024; // 10 MB
        private const int _maxHeaderLineLength = 8192; // 8 KB
        private const int _connectionRetryDelayMilliseconds = 500;

        // Thread ID for the single job execution thread
        private const int _jobThreadId = 1;

        // Frame ID for the current step (always 1)
        private const int _currentFrameId = 1;

        // Frame IDs for completed steps start at 1000
        private const int _completedFrameIdBase = 1000;

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private int _nextSeq = 1;
        private Task _connectionLoopTask;
        private volatile DapSessionState _state = DapSessionState.NotStarted;
        private CancellationTokenRegistration? _cancellationRegistration;
        private bool _isFirstStep = true;

        // Dev Tunnel relay host for remote debugging
        private TunnelRelayTunnelHost _tunnelRelayHost;

        // Cancellation source for the connection loop, cancelled in StopAsync
        // so AcceptTcpClientAsync unblocks cleanly without relying on listener disposal.
        private CancellationTokenSource _loopCts;

        // When true, skip tunnel relay startup (unit tests only)
        internal bool SkipTunnelRelay { get; set; }

        // Synchronization for step execution
        private TaskCompletionSource<DapCommand> _commandTcs;
        private readonly object _stateLock = new object();

        // Session readiness — signaled when configurationDone is received
        private TaskCompletionSource<bool> _readyTcs;

        // Whether to pause before the next step (set by 'next' command)
        private bool _pauseOnNextStep = true;

        // Current execution context
        private IStep _currentStep;
        private IExecutionContext _jobContext;
        private int _currentStepIndex;

        // Track completed steps for stack trace
        private readonly List<CompletedStepInfo> _completedSteps = new List<CompletedStepInfo>();
        private int _nextCompletedFrameId = _completedFrameIdBase;

        // Client connection tracking for reconnection support
        private volatile bool _isClientConnected;

        // Scope/variable inspection provider — reusable by future DAP features
        private DapVariableProvider _variableProvider;

        // REPL command executor for run() commands
        private DapReplExecutor _replExecutor;

        public bool IsActive =>
            _state == DapSessionState.Ready ||
            _state == DapSessionState.Paused ||
            _state == DapSessionState.Running;

        internal DapSessionState State => _state;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _variableProvider = new DapVariableProvider(hostContext.SecretMasker);
            _replExecutor = new DapReplExecutor(hostContext, SendOutput);
            Trace.Info("DapDebugger initialized");
        }

        public async Task StartAsync(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            var debuggerConfig = jobContext.Global.Debugger;

            if (!debuggerConfig.HasValidTunnel)
            {
                throw new ArgumentException(
                    "Debugger requires valid tunnel configuration (tunnelId, clusterId, hostToken, port).");
            }

            Trace.Info($"Starting DAP debugger on port {debuggerConfig.Tunnel.Port}");

            _jobContext = jobContext;
            _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _listener = new TcpListener(IPAddress.Loopback, debuggerConfig.Tunnel.Port);
            _listener.Start();
            Trace.Info($"DAP debugger listening on {_listener.LocalEndpoint}");

            // Start Dev Tunnel relay so remote clients reach the local DAP port.
            // The relay is torn down explicitly in StopAsync (after the DAP session
            // is closed) so we do NOT pass the job cancellation token here — that
            // would race with the DAP shutdown and drop the transport mid-protocol.
            if (!SkipTunnelRelay)
            {
                await StartTunnelRelayAsync(debuggerConfig);
            }

            _state = DapSessionState.WaitingForConnection;
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(jobContext.CancellationToken);
            _connectionLoopTask = ConnectionLoopAsync(_loopCts.Token);

            _cancellationRegistration = jobContext.CancellationToken.Register(() =>
            {
                Trace.Info("Job cancellation requested, unblocking pending waits.");
                _readyTcs?.TrySetCanceled();
                _commandTcs?.TrySetResult(DapCommand.Disconnect);
            });

            Trace.Info($"DAP debugger started on port {debuggerConfig.Tunnel.Port}");
        }

        private async Task StartTunnelRelayAsync(DebuggerConfig config)
        {
            Trace.Info($"Starting Dev Tunnel relay (tunnel={config.Tunnel.TunnelId}, cluster={config.Tunnel.ClusterId})");

            var userAgents = HostContext.UserAgents.ToArray();
            var httpHandler = HostContext.CreateHttpClientHandler();
            httpHandler.AllowAutoRedirect = false;

            var managementClient = new TunnelManagementClient(
                userAgents,
                () => Task.FromResult<AuthenticationHeaderValue>(new AuthenticationHeaderValue("tunnel", config.Tunnel.HostToken)),
                tunnelServiceUri: null,
                httpHandler);

            var tunnel = new Tunnel
            {
                TunnelId = config.Tunnel.TunnelId,
                ClusterId = config.Tunnel.ClusterId,
                AccessTokens = new Dictionary<string, string>
                {
                    [TunnelAccessScopes.Host] = config.Tunnel.HostToken
                },
                Ports = new[]
                {
                    new TunnelPort { PortNumber = config.Tunnel.Port }
                },
            };

            _tunnelRelayHost = new TunnelRelayTunnelHost(managementClient, HostContext.GetTrace("DevTunnelRelay").Source);
            var tunnelConnectTimeoutSeconds = ResolveTunnelConnectTimeout();
            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(tunnelConnectTimeoutSeconds));
            Trace.Info($"Connecting to Dev Tunnel relay (timeout: {tunnelConnectTimeoutSeconds}s)");
            await _tunnelRelayHost.ConnectAsync(tunnel, connectCts.Token);

            Trace.Info("Dev Tunnel relay started");
        }

        public async Task WaitUntilReadyAsync()
        {
            if (_state == DapSessionState.NotStarted || _listener == null || _jobContext == null)
            {
                return;
            }

            var timeoutMinutes = ResolveTimeout();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));

            try
            {
                Trace.Info($"Waiting for debugger client connection (timeout: {timeoutMinutes} minutes)...");
                using (timeoutCts.Token.Register(() => _readyTcs?.TrySetCanceled()))
                {
                    await _readyTcs.Task;
                }

                Trace.Info("DAP debugger ready.");
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !_jobContext.CancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"No debugger client connected within {timeoutMinutes} minutes.");
            }
        }

        public async Task OnJobCompletedAsync()
        {
            if (_state != DapSessionState.NotStarted)
            {
                try
                {
                    OnJobCompleted();
                }
                catch (Exception ex)
                {
                    Trace.Warning($"DAP OnJobCompleted error: {ex.Message}");
                }
            }

            await StopAsync();
        }

        public async Task StopAsync()
        {
            if (_cancellationRegistration.HasValue)
            {
                _cancellationRegistration.Value.Dispose();
                _cancellationRegistration = null;
            }

            try
            {
                if (_listener != null || _tunnelRelayHost != null || _connectionLoopTask != null)
                {
                    Trace.Info("Stopping DAP debugger");
                }

                // Tear down Dev Tunnel relay FIRST — it may hold connections to the
                // local port and must be fully disposed before we release the listener,
                // otherwise the next worker can't bind the same port.
                if (_tunnelRelayHost != null)
                {
                    Trace.Info("Stopping Dev Tunnel relay");
                    var disposeTask = _tunnelRelayHost.DisposeAsync().AsTask();
                    if (await Task.WhenAny(disposeTask, Task.Delay(10_000)) != disposeTask)
                    {
                        Trace.Warning("Dev Tunnel relay dispose timed out after 10s");
                    }
                    else
                    {
                        Trace.Info("Dev Tunnel relay stopped");
                    }

                    _tunnelRelayHost = null;
                }

                CleanupConnection();

                // Cancel the connection loop first so AcceptTcpClientAsync unblocks
                // cleanly, then stop the listener once nothing is using it.
                try { _loopCts?.Cancel(); }
                catch { /* best effort */ }

                try { _listener?.Stop(); }
                catch { /* best effort */ }

                if (_connectionLoopTask != null)
                {
                    try
                    {
                        await Task.WhenAny(_connectionLoopTask, Task.Delay(5000));
                    }
                    catch { /* best effort */ }
                }
            }
            catch (Exception ex)
            {
                Trace.Error("Error stopping DAP debugger");
                Trace.Error(ex);
            }

            lock (_stateLock)
            {
                if (_state != DapSessionState.NotStarted && _state != DapSessionState.Terminated)
                {
                    _state = DapSessionState.Terminated;
                }
            }

            _isClientConnected = false;
            _listener = null;
            _client = null;
            _stream = null;
            _readyTcs = null;
            _connectionLoopTask = null;
            _loopCts?.Dispose();
            _loopCts = null;
        }

        public async Task OnStepStartingAsync(IStep step)
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                bool isFirst = _isFirstStep;
                _isFirstStep = false;
                await OnStepStartingAsync(step, isFirst);
            }
            catch (Exception ex)
            {
                Trace.Warning($"DAP OnStepStarting error: {ex.Message}");
            }
        }

        public void OnStepCompleted(IStep step)
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                var result = step.ExecutionContext?.Result;
                Trace.Info("Step completed");

                // Add to completed steps list for stack trace
                lock (_stateLock)
                {
                    if (_state != DapSessionState.Ready &&
                        _state != DapSessionState.Paused &&
                        _state != DapSessionState.Running)
                    {
                        return;
                    }

                    _completedSteps.Add(new CompletedStepInfo
                    {
                        DisplayName = step.DisplayName,
                        Result = result,
                        FrameId = _nextCompletedFrameId++
                    });
                }
            }
            catch (Exception ex)
            {
                Trace.Warning($"DAP OnStepCompleted error: {ex.Message}");
            }
        }

        internal async Task HandleMessageAsync(string messageJson, CancellationToken cancellationToken)
        {
            Request request = null;
            try
            {
                request = JsonConvert.DeserializeObject<Request>(messageJson);
                if (request == null)
                {
                    Trace.Warning("Failed to deserialize DAP request");
                    return;
                }

                if (!string.Equals(request.Type, "request", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.Warning("Received DAP message that was not a request");
                    return;
                }

                Trace.Info("Handling DAP request");

                Response response;
                if (request.Command == "evaluate")
                {
                    response = await HandleEvaluateAsync(request, cancellationToken);
                }
                else
                {
                    response = request.Command switch
                    {
                        "initialize" => HandleInitialize(request),
                        "attach" => HandleAttach(request),
                        "configurationDone" => HandleConfigurationDone(request),
                        "disconnect" => HandleDisconnect(request),
                        "threads" => HandleThreads(request),
                        "stackTrace" => HandleStackTrace(request),
                        "scopes" => HandleScopes(request),
                        "variables" => HandleVariables(request),
                        "continue" => HandleContinue(request),
                        "next" => HandleNext(request),
                        "setBreakpoints" => HandleSetBreakpoints(request),
                        "setExceptionBreakpoints" => HandleSetExceptionBreakpoints(request),
                        "completions" => HandleCompletions(request),
                        "stepIn" => CreateResponse(request, false, "Step In is not supported. Actions jobs debug at the step level - use 'next' to advance to the next step.", body: null),
                        "stepOut" => CreateResponse(request, false, "Step Out is not supported. Actions jobs debug at the step level - use 'continue' to resume.", body: null),
                        "stepBack" => CreateResponse(request, false, "Step Back is not yet supported.", body: null),
                        "reverseContinue" => CreateResponse(request, false, "Reverse Continue is not yet supported.", body: null),
                        "pause" => CreateResponse(request, false, "Pause is not supported. The debugger pauses automatically at step boundaries.", body: null),
                        _ => CreateResponse(request, false, $"Unsupported command: {request.Command}", body: null)
                    };
                }

                response.RequestSeq = request.Seq;
                response.Command = request.Command;

                SendResponse(response);

                if (request.Command == "initialize")
                {
                    SendEvent(new Event
                    {
                        EventType = "initialized"
                    });
                    Trace.Info("Sent initialized event");
                }
            }
            catch (Exception ex)
            {
                Trace.Error($"Error handling DAP request ({ex.GetType().Name})");
                if (request != null)
                {
                    var maskedMessage = HostContext?.SecretMasker?.MaskSecrets(ex.Message) ?? ex.Message;
                    var errorResponse = CreateResponse(request, false, maskedMessage, body: null);
                    errorResponse.RequestSeq = request.Seq;
                    errorResponse.Command = request.Command;
                    SendResponse(errorResponse);
                }
            }
        }

        internal void HandleClientConnected()
        {
            _isClientConnected = true;
            Trace.Info("Client connected to debug session");

            // If we're paused, re-send the stopped event so the new client
            // knows the current state (important for reconnection)
            string description = null;
            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused && _currentStep != null)
                {
                    description = $"Stopped before step: {_currentStep.DisplayName}";
                }
            }

            if (description != null)
            {
                Trace.Info("Re-sending stopped event to reconnected client");
                SendStoppedEvent("step", description);
            }
        }

        internal void HandleClientDisconnected()
        {
            _isClientConnected = false;
            Trace.Info("Client disconnected from debug session");

            // Intentionally do NOT release the command TCS here.
            // The session stays paused, waiting for a client to reconnect.
            // The debugger's connection loop will accept a new client and
            // call HandleClientConnected, which re-sends the stopped event.
        }

        private async Task ConnectionLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Trace.Info("Waiting for debug client connection...");
                    _client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    _stream = _client.GetStream();
                    var remoteEndPoint = _client.Client.RemoteEndPoint;
                    Trace.Info($"Debug client connected from {remoteEndPoint}");

                    HandleClientConnected();

                    // Enter message processing loop until client disconnects or cancellation is requested
                    await ProcessMessagesAsync(cancellationToken);

                    Trace.Info("Client disconnected, waiting for reconnection...");
                    HandleClientDisconnected();
                    CleanupConnection();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    CleanupConnection();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // If the listener has been stopped, don't retry.
                    if (_listener == null || !_listener.Server.IsBound)
                    {
                        Trace.Info("Listener stopped, exiting connection loop");
                        break;
                    }

                    Trace.Error("Debugger connection error");
                    Trace.Error(ex);

                    try
                    {
                        await Task.Delay(_connectionRetryDelayMilliseconds, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            Trace.Info("Connection loop ended");
        }

        private void CleanupConnection()
        {
            _sendLock.Wait();
            try
            {
                try { _stream?.Close(); } catch { /* best effort */ }
                try { _client?.Close(); } catch { /* best effort */ }
                _stream = null;
                _client = null;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            Trace.Info("Starting DAP message processing loop");

            try
            {
                while (!cancellationToken.IsCancellationRequested && _client?.Connected == true)
                {
                    var json = await ReadMessageAsync(cancellationToken);
                    if (json == null)
                    {
                        Trace.Info("Client disconnected (end of stream)");
                        break;
                    }

                    await HandleMessageAsync(json, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Trace.Info("Message processing cancelled");
            }
            catch (IOException ex)
            {
                Trace.Info($"Connection closed ({ex.GetType().Name})");
            }
            catch (Exception ex)
            {
                Trace.Error($"Error in message loop ({ex.GetType().Name})");
            }

            Trace.Info("DAP message processing loop ended");
        }

        private async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            int contentLength = -1;

            while (true)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    return null;
                }

                if (line.Length == 0)
                {
                    break;
                }

                if (line.StartsWith(_contentLengthHeader, StringComparison.OrdinalIgnoreCase))
                {
                    var lengthStr = line.Substring(_contentLengthHeader.Length).Trim();
                    if (!int.TryParse(lengthStr, out contentLength))
                    {
                        throw new InvalidDataException($"Invalid Content-Length: {lengthStr}");
                    }
                }
            }

            if (contentLength < 0)
            {
                throw new InvalidDataException("Missing Content-Length header");
            }

            if (contentLength > _maxMessageSize)
            {
                throw new InvalidDataException($"Message size {contentLength} exceeds maximum allowed size of {_maxMessageSize}");
            }

            var buffer = new byte[contentLength];
            var totalRead = 0;
            while (totalRead < contentLength)
            {
                var bytesRead = await _stream.ReadAsync(buffer, totalRead, contentLength - totalRead, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Connection closed while reading message body");
                }
                totalRead += bytesRead;
            }

            var json = Encoding.UTF8.GetString(buffer);
            Trace.Verbose("Received DAP message body");
            return json;
        }

        private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            var lineBuilder = new StringBuilder();
            var buffer = new byte[1];
            var previousWasCr = false;

            while (true)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, 1, cancellationToken);
                if (bytesRead == 0)
                {
                    return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                }

                var c = (char)buffer[0];

                if (c == '\n' && previousWasCr)
                {
                    if (lineBuilder.Length > 0 && lineBuilder[lineBuilder.Length - 1] == '\r')
                    {
                        lineBuilder.Length--;
                    }
                    return lineBuilder.ToString();
                }

                previousWasCr = c == '\r';
                lineBuilder.Append(c);

                if (lineBuilder.Length > _maxHeaderLineLength)
                {
                    throw new InvalidDataException($"Header line exceeds maximum length of {_maxHeaderLineLength}");
                }
            }
        }

        /// <summary>
        /// Serializes and writes a DAP message with Content-Length framing.
        /// Must be called within the _sendLock.
        ///
        /// Secret masking is intentionally NOT applied here at the serialization
        /// layer. Masking the raw JSON would corrupt protocol envelope fields
        /// (type, event, command, seq) if a secret collides with those strings.
        /// Instead, each DAP producer masks user-visible text at the point of
        /// construction via the runner's SecretMasker. See DapVariableProvider,
        /// DapReplExecutor, and DapDebugger for the call sites.
        /// </summary>
        private void SendMessageInternal(ProtocolMessage message)
        {
            var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var bodyBytes = Encoding.UTF8.GetBytes(json);
            var header = $"Content-Length: {bodyBytes.Length}\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);

            _stream.Write(headerBytes, 0, headerBytes.Length);
            _stream.Write(bodyBytes, 0, bodyBytes.Length);
            _stream.Flush();

            Trace.Verbose("Sent DAP message");
        }

        private void SendMessage(ProtocolMessage message)
        {
            try
            {
                _sendLock.Wait();
                try
                {
                    if (_stream == null)
                    {
                        Trace.Warning("Cannot send message: no client connected");
                        return;
                    }

                    message.Seq = _nextSeq++;
                    SendMessageInternal(message);
                }
                finally
                {
                    _sendLock.Release();
                }

                Trace.Info("Sent message");
            }
            catch (Exception ex)
            {
                Trace.Warning($"Failed to send message ({ex.GetType().Name})");
            }
        }

        private void SendEvent(Event evt)
        {
            SendMessage(evt);
        }

        private void SendResponse(Response response)
        {
            SendMessage(response);
        }

        private void SendOutput(string category, string text)
        {
            SendEvent(new Event
            {
                EventType = "output",
                Body = new OutputEventBody
                {
                    Category = category,
                    Output = text
                }
            });
        }

        internal async Task OnStepStartingAsync(IStep step, bool isFirstStep)
        {
            bool pauseOnNextStep;
            CancellationToken cancellationToken;
            lock (_stateLock)
            {
                if (_state != DapSessionState.Ready &&
                    _state != DapSessionState.Paused &&
                    _state != DapSessionState.Running)
                {
                    return;
                }

                _currentStep = step;
                _currentStepIndex = _completedSteps.Count;
                pauseOnNextStep = _pauseOnNextStep;
                cancellationToken = _jobContext?.CancellationToken ?? CancellationToken.None;
            }

            // Reset variable references so stale nested refs from the
            // previous step are not served to the client.
            _variableProvider?.Reset();

            // Determine if we should pause
            bool shouldPause = isFirstStep || pauseOnNextStep;

            if (!shouldPause)
            {
                Trace.Info("Step starting without debugger pause");
                return;
            }

            var reason = isFirstStep ? "entry" : "step";
            var description = isFirstStep
                ? $"Stopped at job entry: {step.DisplayName}"
                : $"Stopped before step: {step.DisplayName}";

            Trace.Info("Step starting with debugger pause");

            // Send stopped event to debugger (only if client is connected)
            SendStoppedEvent(reason, description);

            // Wait for debugger command
            await WaitForCommandAsync(cancellationToken);
        }

        internal void OnJobCompleted()
        {
            Trace.Info("Job completed, sending terminated event");

            int exitCode;
            lock (_stateLock)
            {
                if (_state == DapSessionState.Terminated)
                {
                    Trace.Info("Session already terminated, skipping OnJobCompleted events");
                    return;
                }
                _state = DapSessionState.Terminated;
                exitCode = _jobContext?.Result == TaskResult.Succeeded ? 0 : 1;
            }

            SendEvent(new Event
            {
                EventType = "terminated",
                Body = new TerminatedEventBody()
            });

            SendEvent(new Event
            {
                EventType = "exited",
                Body = new ExitedEventBody
                {
                    ExitCode = exitCode
                }
            });
        }

        private Response HandleInitialize(Request request)
        {
            if (request.Arguments != null)
            {
                try
                {
                    request.Arguments.ToObject<InitializeRequestArguments>();
                    Trace.Info("Initialize arguments received");
                }
                catch (Exception ex)
                {
                    Trace.Warning($"Failed to parse initialize arguments ({ex.GetType().Name})");
                }
            }

            lock (_stateLock)
            {
                _state = DapSessionState.Initializing;
            }

            // Build capabilities — MVP only supports configurationDone
            var capabilities = new Capabilities
            {
                SupportsConfigurationDoneRequest = true,
                SupportsEvaluateForHovers = true,

                // All other capabilities are false for MVP
                SupportsFunctionBreakpoints = false,
                SupportsConditionalBreakpoints = false,
                SupportsStepBack = false,
                SupportsSetVariable = false,
                SupportsRestartFrame = false,
                SupportsGotoTargetsRequest = false,
                SupportsStepInTargetsRequest = false,
                SupportsCompletionsRequest = true,
                SupportsModulesRequest = false,
                SupportsTerminateRequest = false,
                SupportTerminateDebuggee = false,
                SupportsDelayedStackTraceLoading = false,
                SupportsLoadedSourcesRequest = false,
                SupportsProgressReporting = false,
                SupportsRunInTerminalRequest = false,
                SupportsCancelRequest = false,
                SupportsExceptionOptions = false,
                SupportsValueFormattingOptions = false,
                SupportsExceptionInfoRequest = false,
            };

            Trace.Info("Initialize request handled, capabilities sent");
            return CreateResponse(request, true, body: capabilities);
        }

        private Response HandleAttach(Request request)
        {
            Trace.Info("Attach request handled");
            return CreateResponse(request, true, body: null);
        }

        private Response HandleConfigurationDone(Request request)
        {
            lock (_stateLock)
            {
                _state = DapSessionState.Ready;
            }

            _readyTcs.TrySetResult(true);

            Trace.Info("Configuration done, debug session is ready");
            return CreateResponse(request, true, body: null);
        }

        private Response HandleDisconnect(Request request)
        {
            Trace.Info("Disconnect request received");

            lock (_stateLock)
            {
                _state = DapSessionState.Terminated;

                // Release any blocked step execution
                _commandTcs?.TrySetResult(DapCommand.Disconnect);
            }

            return CreateResponse(request, true, body: null);
        }

        private Response HandleThreads(Request request)
        {
            IExecutionContext jobContext;
            lock (_stateLock)
            {
                jobContext = _jobContext;
            }

            var threadName = jobContext != null
                ? MaskUserVisibleText($"Job: {jobContext.GetGitHubContext("job") ?? "workflow job"}")
                : "Job Thread";

            var body = new ThreadsResponseBody
            {
                Threads = new List<Thread>
                {
                    new Thread
                    {
                        Id = _jobThreadId,
                        Name = threadName
                    }
                }
            };

            return CreateResponse(request, true, body: body);
        }

        private Response HandleStackTrace(Request request)
        {
            IStep currentStep;
            int currentStepIndex;
            CompletedStepInfo[] completedSteps;
            lock (_stateLock)
            {
                currentStep = _currentStep;
                currentStepIndex = _currentStepIndex;
                completedSteps = _completedSteps.ToArray();
            }

            var frames = new List<StackFrame>();

            // Add current step as the top frame
            if (currentStep != null)
            {
                var resultIndicator = currentStep.ExecutionContext?.Result != null
                    ? $" [{currentStep.ExecutionContext.Result}]"
                    : " [running]";

                frames.Add(new StackFrame
                {
                    Id = _currentFrameId,
                    Name = MaskUserVisibleText($"{currentStep.DisplayName ?? "Current Step"}{resultIndicator}"),
                    Line = currentStepIndex + 1,
                    Column = 1,
                    PresentationHint = "normal"
                });
            }
            else
            {
                frames.Add(new StackFrame
                {
                    Id = _currentFrameId,
                    Name = "(no step executing)",
                    Line = 0,
                    Column = 1,
                    PresentationHint = "subtle"
                });
            }

            // Add completed steps as additional frames (most recent first)
            for (int i = completedSteps.Length - 1; i >= 0; i--)
            {
                var completedStep = completedSteps[i];
                var resultStr = completedStep.Result.HasValue ? $" [{completedStep.Result}]" : "";
                frames.Add(new StackFrame
                {
                    Id = completedStep.FrameId,
                    Name = MaskUserVisibleText($"{completedStep.DisplayName}{resultStr}"),
                    Line = 1,
                    Column = 1,
                    PresentationHint = "subtle"
                });
            }

            var body = new StackTraceResponseBody
            {
                StackFrames = frames,
                TotalFrames = frames.Count
            };

            return CreateResponse(request, true, body: body);
        }

        private Response HandleScopes(Request request)
        {
            var args = request.Arguments?.ToObject<ScopesArguments>();
            var frameId = args?.FrameId ?? _currentFrameId;

            var context = GetExecutionContextForFrame(frameId);
            if (context == null)
            {
                return CreateResponse(request, true, body: new ScopesResponseBody
                {
                    Scopes = new List<Scope>()
                });
            }

            var scopes = _variableProvider.GetScopes(context);
            return CreateResponse(request, true, body: new ScopesResponseBody
            {
                Scopes = scopes
            });
        }

        private Response HandleVariables(Request request)
        {
            var args = request.Arguments?.ToObject<VariablesArguments>();
            var variablesRef = args?.VariablesReference ?? 0;

            var context = GetCurrentExecutionContext();
            if (context == null)
            {
                return CreateResponse(request, true, body: new VariablesResponseBody
                {
                    Variables = new List<Variable>()
                });
            }

            var variables = _variableProvider.GetVariables(context, variablesRef);
            return CreateResponse(request, true, body: new VariablesResponseBody
            {
                Variables = variables
            });
        }

        private async Task<Response> HandleEvaluateAsync(Request request, CancellationToken cancellationToken)
        {
            var args = request.Arguments?.ToObject<EvaluateArguments>();
            var expression = args?.Expression ?? string.Empty;
            var frameId = args?.FrameId ?? _currentFrameId;
            var evalContext = args?.Context ?? "hover";

            Trace.Info("Evaluate request received");

            // REPL context -> route through the DSL dispatcher
            if (string.Equals(evalContext, "repl", StringComparison.OrdinalIgnoreCase))
            {
                var result = await HandleReplInputAsync(expression, frameId, cancellationToken);
                return CreateResponse(request, true, body: result);
            }

            // Watch/hover/variables/clipboard -> expression evaluation only
            var context = GetExecutionContextForFrame(frameId);
            var evalResult = _variableProvider.EvaluateExpression(expression, context);
            return CreateResponse(request, true, body: evalResult);
        }

        /// <summary>
        /// Routes REPL input through the DSL parser. If the input matches a
        /// known command it is dispatched; otherwise it falls through to
        /// expression evaluation.
        /// </summary>
        private async Task<EvaluateResponseBody> HandleReplInputAsync(
            string input,
            int frameId,
            CancellationToken cancellationToken)
        {
            // Try to parse as a DSL command
            var command = DapReplParser.TryParse(input, out var parseError);

            if (parseError != null)
            {
                return new EvaluateResponseBody
                {
                    Result = parseError,
                    Type = "error",
                    VariablesReference = 0
                };
            }

            if (command != null)
            {
                return await DispatchReplCommandAsync(command, frameId, cancellationToken);
            }

            // Not a DSL command -> evaluate as a GitHub Actions expression
            // (this lets the REPL console also work for ad-hoc expression queries)
            var context = GetExecutionContextForFrame(frameId);
            return _variableProvider.EvaluateExpression(input, context);
        }

        private async Task<EvaluateResponseBody> DispatchReplCommandAsync(
            DapReplCommand command,
            int frameId,
            CancellationToken cancellationToken)
        {
            switch (command)
            {
                case HelpCommand help:
                    var helpText = string.IsNullOrEmpty(help.Topic)
                        ? DapReplParser.GetGeneralHelp()
                        : help.Topic.Equals("run", StringComparison.OrdinalIgnoreCase)
                            ? DapReplParser.GetRunHelp()
                            : $"Unknown help topic: {help.Topic}. Try: help or help(\"run\")";
                    return new EvaluateResponseBody
                    {
                        Result = helpText,
                        Type = "string",
                        VariablesReference = 0
                    };

                case RunCommand run:
                    var context = GetExecutionContextForFrame(frameId);
                    return await _replExecutor.ExecuteRunCommandAsync(run, context, cancellationToken);

                default:
                    return new EvaluateResponseBody
                    {
                        Result = $"Unknown command type: {command.GetType().Name}",
                        Type = "error",
                        VariablesReference = 0
                    };
            }
        }

        private Response HandleCompletions(Request request)
        {
            var args = request.Arguments?.ToObject<CompletionsArguments>();
            var text = args?.Text ?? string.Empty;

            var items = new List<CompletionItem>();

            // Offer DSL commands when the user is starting to type
            if (string.IsNullOrEmpty(text) || "help".StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new CompletionItem
                {
                    Label = "help",
                    Text = "help",
                    Detail = "Show available debug console commands",
                    Type = "function"
                });
            }
            if (string.IsNullOrEmpty(text) || "help(\"run\")".StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new CompletionItem
                {
                    Label = "help(\"run\")",
                    Text = "help(\"run\")",
                    Detail = "Show help for the run command",
                    Type = "function"
                });
            }
            if (string.IsNullOrEmpty(text) || "run(".StartsWith(text, StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("run(", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new CompletionItem
                {
                    Label = "run(\"...\")",
                    Text = "run(\"",
                    Detail = "Execute a script (like a workflow run step)",
                    Type = "function"
                });
            }

            return CreateResponse(request, true, body: new CompletionsResponseBody
            {
                Targets = items
            });
        }

        private Response HandleContinue(Request request)
        {
            Trace.Info("Continue command received");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                    _pauseOnNextStep = false;
                    _commandTcs?.TrySetResult(DapCommand.Continue);
                }
            }

            return CreateResponse(request, true, body: new ContinueResponseBody
            {
                AllThreadsContinued = true
            });
        }

        private Response HandleNext(Request request)
        {
            Trace.Info("Next (step over) command received");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                    _pauseOnNextStep = true;
                    _commandTcs?.TrySetResult(DapCommand.Next);
                }
            }

            return CreateResponse(request, true, body: null);
        }

        private Response HandleSetBreakpoints(Request request)
        {
            // MVP: acknowledge but don't process breakpoints
            // All steps pause automatically via _pauseOnNextStep
            return CreateResponse(request, true, body: new { breakpoints = Array.Empty<object>() });
        }

        private Response HandleSetExceptionBreakpoints(Request request)
        {
            // MVP: acknowledge but don't process exception breakpoints
            return CreateResponse(request, true, body: null);
        }

        /// <summary>
        /// Blocks the step execution thread until a debugger command is received
        /// or the job is cancelled. Job cancellation is handled by the registration
        /// in StartAsync which sets _commandTcs to Disconnect.
        /// </summary>
        private async Task WaitForCommandAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                if (_state == DapSessionState.Terminated)
                {
                    return;
                }
                _state = DapSessionState.Paused;
                _commandTcs = new TaskCompletionSource<DapCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Trace.Info("Waiting for debugger command...");

            var command = await _commandTcs.Task;

            Trace.Info("Received debugger command");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                }
            }

            // Send continued event for normal flow commands
            if (!cancellationToken.IsCancellationRequested &&
                (command == DapCommand.Continue || command == DapCommand.Next))
            {
                SendEvent(new Event
                {
                    EventType = "continued",
                    Body = new ContinuedEventBody
                    {
                        ThreadId = _jobThreadId,
                        AllThreadsContinued = true
                    }
                });
            }
        }

        /// <summary>
        /// Resolves the execution context for a given stack frame ID.
        /// Frame 1 = current step; frames 1000+ = completed steps (no
        /// context available - those steps have already finished).
        /// Falls back to the job-level context when no step is active.
        /// </summary>
        private IExecutionContext GetExecutionContextForFrame(int frameId)
        {
            if (frameId == _currentFrameId)
            {
                return GetCurrentExecutionContext();
            }

            // Completed-step frames don't carry a live execution context.
            return null;
        }

        private IExecutionContext GetCurrentExecutionContext()
        {
            lock (_stateLock)
            {
                return _currentStep?.ExecutionContext ?? _jobContext;
            }
        }

        /// <summary>
        /// Sends a stopped event to the connected client.
        /// Silently no-ops if no client is connected.
        /// </summary>
        private void SendStoppedEvent(string reason, string description)
        {
            if (!_isClientConnected)
            {
                Trace.Info("No client connected, deferring stopped event");
                return;
            }

            SendEvent(new Event
            {
                EventType = "stopped",
                Body = new StoppedEventBody
                {
                    Reason = reason,
                    Description = MaskUserVisibleText(description),
                    ThreadId = _jobThreadId,
                    AllThreadsStopped = true
                }
            });
        }

        private string MaskUserVisibleText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return HostContext?.SecretMasker?.MaskSecrets(value) ?? value;
        }

        /// <summary>
        /// Creates a DAP response with common fields pre-populated.
        /// </summary>
        private Response CreateResponse(Request request, bool success, string message = null, object body = null)
        {
            return new Response
            {
                Type = "response",
                RequestSeq = request.Seq,
                Command = request.Command,
                Success = success,
                Message = success ? null : message,
                Body = body
            };
        }

        internal int ResolveTimeout()
        {
            var timeoutEnv = Environment.GetEnvironmentVariable(_timeoutEnvironmentVariable);
            if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var customTimeout) && customTimeout > 0)
            {
                Trace.Info($"Using custom DAP timeout {customTimeout} minutes from {_timeoutEnvironmentVariable}");
                return customTimeout;
            }

            return _defaultTimeoutMinutes;
        }

        internal int ResolveTunnelConnectTimeout()
        {
            var raw = Environment.GetEnvironmentVariable(_tunnelConnectTimeoutSeconds);
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var customTimeout) && customTimeout > 0)
            {
                Trace.Info($"Using custom tunnel connect timeout {customTimeout}s from {_tunnelConnectTimeoutSeconds}");
                return customTimeout;
            }

            return _defaultTunnelConnectTimeoutSeconds;
        }
    }
}
