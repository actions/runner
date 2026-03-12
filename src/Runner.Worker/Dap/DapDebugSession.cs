using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
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
    /// Production DAP debug session.
    /// Handles step-level breakpoints with next/continue flow control,
    /// scope/variable inspection, client reconnection, and cancellation
    /// signal propagation.
    ///
    /// REPL, step manipulation, and time-travel debugging are intentionally
    /// deferred to future iterations.
    /// </summary>
    public sealed class DapDebugSession : RunnerService, IDapDebugSession
    {
        // Thread ID for the single job execution thread
        private const int JobThreadId = 1;

        // Frame ID for the current step (always 1)
        private const int CurrentFrameId = 1;

        // Frame IDs for completed steps start at 1000
        private const int CompletedFrameIdBase = 1000;

        private IDapServer _server;
        private DapSessionState _state = DapSessionState.WaitingForConnection;

        // Synchronization for step execution
        private TaskCompletionSource<DapCommand> _commandTcs;
        private readonly object _stateLock = new object();

        // Handshake completion — signaled when configurationDone is received
        private readonly TaskCompletionSource<bool> _handshakeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Whether to pause before the next step (set by 'next' command)
        private bool _pauseOnNextStep = true;

        // Current execution context
        private IStep _currentStep;
        private IExecutionContext _jobContext;
        private int _currentStepIndex;

        // Track completed steps for stack trace
        private readonly List<CompletedStepInfo> _completedSteps = new List<CompletedStepInfo>();
        private int _nextCompletedFrameId = CompletedFrameIdBase;

        // Client connection tracking for reconnection support
        private volatile bool _isClientConnected;

        // Scope/variable inspection provider — reusable by future DAP features
        private DapVariableProvider _variableProvider;

        public bool IsActive =>
            _state == DapSessionState.Ready ||
            _state == DapSessionState.Paused ||
            _state == DapSessionState.Running;

        public DapSessionState State => _state;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _variableProvider = new DapVariableProvider(hostContext);
            Trace.Info("DapDebugSession initialized");
        }

        public void SetDapServer(IDapServer server)
        {
            _server = server;
            Trace.Info("DAP server reference set");
        }

        public async Task WaitForHandshakeAsync(CancellationToken cancellationToken)
        {
            Trace.Info("Waiting for DAP handshake (configurationDone)...");

            using (cancellationToken.Register(() => _handshakeTcs.TrySetCanceled()))
            {
                await _handshakeTcs.Task;
            }

            Trace.Info("DAP handshake complete, session is ready");
        }

        #region Message Dispatch

        public async Task HandleMessageAsync(string messageJson, CancellationToken cancellationToken)
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

                Trace.Info($"Handling DAP request: {request.Command}");

                var response = request.Command switch
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
                    _ => CreateResponse(request, false, $"Unsupported command: {request.Command}", body: null)
                };

                response.RequestSeq = request.Seq;
                response.Command = request.Command;

                _server?.SendResponse(response);
            }
            catch (Exception ex)
            {
                Trace.Error($"Error handling request '{request?.Command}': {ex}");
                if (request != null)
                {
                    var errorResponse = CreateResponse(request, false, ex.Message, body: null);
                    errorResponse.RequestSeq = request.Seq;
                    errorResponse.Command = request.Command;
                    _server?.SendResponse(errorResponse);
                }
            }

            await Task.CompletedTask;
        }

        #endregion

        #region DAP Request Handlers

        private Response HandleInitialize(Request request)
        {
            if (request.Arguments != null)
            {
                try
                {
                    var clientCaps = request.Arguments.ToObject<InitializeRequestArguments>();
                    Trace.Info($"Client: {clientCaps?.ClientName ?? clientCaps?.ClientId ?? "unknown"}");
                }
                catch (Exception ex)
                {
                    Trace.Warning($"Failed to parse initialize arguments: {ex.Message}");
                }
            }

            _state = DapSessionState.Initializing;

            // Build capabilities — MVP only supports configurationDone
            var capabilities = new Capabilities
            {
                SupportsConfigurationDoneRequest = true,
                // All other capabilities are false for MVP
                SupportsFunctionBreakpoints = false,
                SupportsConditionalBreakpoints = false,
                SupportsEvaluateForHovers = false,
                SupportsStepBack = false,
                SupportsSetVariable = false,
                SupportsRestartFrame = false,
                SupportsGotoTargetsRequest = false,
                SupportsStepInTargetsRequest = false,
                SupportsCompletionsRequest = false,
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

            // Send initialized event after a brief delay to ensure the
            // response is delivered first (DAP spec requirement)
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                _server?.SendEvent(new Event
                {
                    EventType = "initialized"
                });
                Trace.Info("Sent initialized event");
            });

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

            _handshakeTcs.TrySetResult(true);

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
            var body = new ThreadsResponseBody
            {
                Threads = new List<Thread>
                {
                    new Thread
                    {
                        Id = JobThreadId,
                        Name = _jobContext != null
                            ? $"Job: {_jobContext.GetGitHubContext("job") ?? "workflow job"}"
                            : "Job Thread"
                    }
                }
            };

            return CreateResponse(request, true, body: body);
        }

        private Response HandleStackTrace(Request request)
        {
            var frames = new List<StackFrame>();

            // Add current step as the top frame
            if (_currentStep != null)
            {
                var resultIndicator = _currentStep.ExecutionContext?.Result != null
                    ? $" [{_currentStep.ExecutionContext.Result}]"
                    : " [running]";

                frames.Add(new StackFrame
                {
                    Id = CurrentFrameId,
                    Name = $"{_currentStep.DisplayName ?? "Current Step"}{resultIndicator}",
                    Line = _currentStepIndex + 1,
                    Column = 1,
                    PresentationHint = "normal"
                });
            }
            else
            {
                frames.Add(new StackFrame
                {
                    Id = CurrentFrameId,
                    Name = "(no step executing)",
                    Line = 0,
                    Column = 1,
                    PresentationHint = "subtle"
                });
            }

            // Add completed steps as additional frames (most recent first)
            for (int i = _completedSteps.Count - 1; i >= 0; i--)
            {
                var completedStep = _completedSteps[i];
                var resultStr = completedStep.Result.HasValue ? $" [{completedStep.Result}]" : "";
                frames.Add(new StackFrame
                {
                    Id = completedStep.FrameId,
                    Name = $"{completedStep.DisplayName}{resultStr}",
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
            var frameId = args?.FrameId ?? CurrentFrameId;

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

            var context = _currentStep?.ExecutionContext ?? _jobContext;
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

        #endregion

        #region Step Lifecycle

        public async Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken)
        {
            if (!IsActive)
            {
                return;
            }

            _currentStep = step;
            _jobContext = jobContext;
            _currentStepIndex = _completedSteps.Count;

            // Reset variable references so stale nested refs from the
            // previous step are not served to the client.
            _variableProvider?.Reset();

            // Determine if we should pause
            bool shouldPause = isFirstStep || _pauseOnNextStep;

            if (!shouldPause)
            {
                Trace.Info($"Step starting (not pausing): {step.DisplayName}");
                return;
            }

            var reason = isFirstStep ? "entry" : "step";
            var description = isFirstStep
                ? $"Stopped at job entry: {step.DisplayName}"
                : $"Stopped before step: {step.DisplayName}";

            Trace.Info($"Step starting: {step.DisplayName} (reason: {reason})");

            // Send stopped event to debugger (only if client is connected)
            SendStoppedEvent(reason, description);

            // Wait for debugger command
            await WaitForCommandAsync(cancellationToken);
        }

        public void OnStepCompleted(IStep step)
        {
            if (!IsActive)
            {
                return;
            }

            var result = step.ExecutionContext?.Result;
            Trace.Info($"Step completed: {step.DisplayName}, result: {result}");

            // Add to completed steps list for stack trace
            _completedSteps.Add(new CompletedStepInfo
            {
                DisplayName = step.DisplayName,
                Result = result,
                FrameId = _nextCompletedFrameId++
            });
        }

        public void OnJobCompleted()
        {
            if (!IsActive)
            {
                return;
            }

            Trace.Info("Job completed, sending terminated event");

            lock (_stateLock)
            {
                _state = DapSessionState.Terminated;
            }

            _server?.SendEvent(new Event
            {
                EventType = "terminated",
                Body = new TerminatedEventBody()
            });

            var exitCode = _jobContext?.Result == TaskResult.Succeeded ? 0 : 1;
            _server?.SendEvent(new Event
            {
                EventType = "exited",
                Body = new ExitedEventBody
                {
                    ExitCode = exitCode
                }
            });
        }

        public void CancelSession()
        {
            Trace.Info("CancelSession called - terminating debug session");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Terminated)
                {
                    Trace.Info("Session already terminated, ignoring CancelSession");
                    return;
                }
                _state = DapSessionState.Terminated;
            }

            // Send terminated event to debugger so it updates its UI
            _server?.SendEvent(new Event
            {
                EventType = "terminated",
                Body = new TerminatedEventBody()
            });

            // Send exited event with cancellation exit code (130 = SIGINT convention)
            _server?.SendEvent(new Event
            {
                EventType = "exited",
                Body = new ExitedEventBody { ExitCode = 130 }
            });

            // Release any pending command waits
            _commandTcs?.TrySetResult(DapCommand.Disconnect);

            // Release handshake wait if still pending
            _handshakeTcs.TrySetCanceled();

            Trace.Info("Debug session cancelled");
        }

        #endregion

        #region Client Connection Tracking

        public void HandleClientConnected()
        {
            _isClientConnected = true;
            Trace.Info("Client connected to debug session");

            // If we're paused, re-send the stopped event so the new client
            // knows the current state (important for reconnection)
            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused && _currentStep != null)
                {
                    Trace.Info("Re-sending stopped event to reconnected client");
                    var description = $"Stopped before step: {_currentStep.DisplayName}";
                    SendStoppedEvent("step", description);
                }
            }
        }

        public void HandleClientDisconnected()
        {
            _isClientConnected = false;
            Trace.Info("Client disconnected from debug session");

            // Intentionally do NOT release the command TCS here.
            // The session stays paused, waiting for a client to reconnect.
            // The server's connection loop will accept a new client and
            // call HandleClientConnected, which re-sends the stopped event.
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Blocks the step execution thread until a debugger command is received
        /// or the job is cancelled.
        /// </summary>
        private async Task WaitForCommandAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                _state = DapSessionState.Paused;
                _commandTcs = new TaskCompletionSource<DapCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Trace.Info("Waiting for debugger command...");

            using (cancellationToken.Register(() =>
            {
                Trace.Info("Job cancellation detected, releasing debugger wait");
                _commandTcs?.TrySetResult(DapCommand.Disconnect);
            }))
            {
                var command = await _commandTcs.Task;

                Trace.Info($"Received command: {command}");

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
                    _server?.SendEvent(new Event
                    {
                        EventType = "continued",
                        Body = new ContinuedEventBody
                        {
                            ThreadId = JobThreadId,
                            AllThreadsContinued = true
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Resolves the execution context for a given stack frame ID.
        /// Frame 1 = current step; frames 1000+ = completed steps (no
        /// context available — those steps have already finished).
        /// Falls back to the job-level context when no step is active.
        /// </summary>
        private IExecutionContext GetExecutionContextForFrame(int frameId)
        {
            if (frameId == CurrentFrameId)
            {
                return _currentStep?.ExecutionContext ?? _jobContext;
            }

            // Completed-step frames don't carry a live execution context.
            return null;
        }

        /// <summary>
        /// Sends a stopped event to the connected client.
        /// Silently no-ops if no client is connected.
        /// </summary>
        private void SendStoppedEvent(string reason, string description)
        {
            if (!_isClientConnected)
            {
                Trace.Info($"No client connected, deferring stopped event: {description}");
                return;
            }

            _server?.SendEvent(new Event
            {
                EventType = "stopped",
                Body = new StoppedEventBody
                {
                    Reason = reason,
                    Description = description,
                    ThreadId = JobThreadId,
                    AllThreadsStopped = true
                }
            });
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

        #endregion
    }
}
