using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Debug session state machine states.
    /// </summary>
    public enum DapSessionState
    {
        /// <summary>
        /// Initial state, waiting for client connection.
        /// </summary>
        WaitingForConnection,

        /// <summary>
        /// Client connected, exchanging capabilities.
        /// </summary>
        Initializing,

        /// <summary>
        /// ConfigurationDone received, ready to debug.
        /// </summary>
        Ready,

        /// <summary>
        /// Paused before or after a step, waiting for user command.
        /// </summary>
        Paused,

        /// <summary>
        /// Executing a step.
        /// </summary>
        Running,

        /// <summary>
        /// Session disconnected or terminated.
        /// </summary>
        Terminated
    }

    /// <summary>
    /// Commands that can be issued from the debug client.
    /// </summary>
    public enum DapCommand
    {
        /// <summary>
        /// Continue execution until end or next breakpoint.
        /// </summary>
        Continue,

        /// <summary>
        /// Execute current step and pause before next.
        /// </summary>
        Next,

        /// <summary>
        /// Pause execution.
        /// </summary>
        Pause,

        /// <summary>
        /// Disconnect from the debug session.
        /// </summary>
        Disconnect
    }

    /// <summary>
    /// Reasons for stopping/pausing execution.
    /// </summary>
    public static class StopReason
    {
        public const string Entry = "entry";
        public const string Step = "step";
        public const string Breakpoint = "breakpoint";
        public const string Pause = "pause";
        public const string Exception = "exception";
    }

    /// <summary>
    /// Interface for the DAP debug session.
    /// Handles debug state, step coordination, and DAP request processing.
    /// </summary>
    [ServiceLocator(Default = typeof(DapDebugSession))]
    public interface IDapDebugSession : IRunnerService
    {
        /// <summary>
        /// Gets whether the debug session is active (initialized and configured).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the current session state.
        /// </summary>
        DapSessionState State { get; }

        /// <summary>
        /// Sets the DAP server for sending events.
        /// </summary>
        /// <param name="server">The DAP server</param>
        void SetDapServer(IDapServer server);

        /// <summary>
        /// Handles an incoming DAP request and returns a response.
        /// </summary>
        /// <param name="request">The DAP request</param>
        /// <returns>The DAP response</returns>
        Task<Response> HandleRequestAsync(Request request);

        /// <summary>
        /// Called by StepsRunner before a step starts executing.
        /// May block waiting for debugger commands.
        /// </summary>
        /// <param name="step">The step about to execute</param>
        /// <param name="jobContext">The job execution context</param>
        /// <param name="isFirstStep">Whether this is the first step in the job</param>
        /// <returns>Task that completes when execution should continue</returns>
        Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep);

        /// <summary>
        /// Called by StepsRunner after a step completes.
        /// </summary>
        /// <param name="step">The step that completed</param>
        void OnStepCompleted(IStep step);

        /// <summary>
        /// Notifies the session that the job has completed.
        /// </summary>
        void OnJobCompleted();
    }

    /// <summary>
    /// Debug session implementation for handling DAP requests and coordinating
    /// step execution with the debugger.
    /// </summary>
    public sealed class DapDebugSession : RunnerService, IDapDebugSession
    {
        // Thread ID for the single job execution thread
        private const int JobThreadId = 1;

        // Frame ID for the current step
        private const int CurrentFrameId = 1;

        private IDapServer _server;
        private DapSessionState _state = DapSessionState.WaitingForConnection;
        private InitializeRequestArguments _clientCapabilities;

        // Synchronization for step execution
        private TaskCompletionSource<DapCommand> _commandTcs;
        private readonly object _stateLock = new object();

        // Current execution context (set during OnStepStartingAsync)
        private IStep _currentStep;
        private IExecutionContext _jobContext;

        public bool IsActive => _state == DapSessionState.Ready || _state == DapSessionState.Paused || _state == DapSessionState.Running;

        public DapSessionState State => _state;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Trace.Info("DapDebugSession initialized");
        }

        public void SetDapServer(IDapServer server)
        {
            _server = server;
            Trace.Info("DAP server reference set");
        }

        public async Task<Response> HandleRequestAsync(Request request)
        {
            Trace.Info($"Handling DAP request: {request.Command}");

            try
            {
                return request.Command switch
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
                    "pause" => HandlePause(request),
                    "evaluate" => await HandleEvaluateAsync(request),
                    "setBreakpoints" => HandleSetBreakpoints(request),
                    "setExceptionBreakpoints" => HandleSetExceptionBreakpoints(request),
                    _ => CreateErrorResponse($"Unknown command: {request.Command}")
                };
            }
            catch (Exception ex)
            {
                Trace.Error($"Error handling request '{request.Command}': {ex}");
                return CreateErrorResponse(ex.Message);
            }
        }

        #region DAP Request Handlers

        private Response HandleInitialize(Request request)
        {
            // Parse client capabilities
            if (request.Arguments != null)
            {
                _clientCapabilities = request.Arguments.ToObject<InitializeRequestArguments>();
                Trace.Info($"Client: {_clientCapabilities.ClientName ?? _clientCapabilities.ClientId ?? "unknown"}");
            }

            _state = DapSessionState.Initializing;

            // Build our capabilities response
            var capabilities = new Capabilities
            {
                SupportsConfigurationDoneRequest = true,
                SupportsEvaluateForHovers = true,
                SupportTerminateDebuggee = true,
                SupportsTerminateRequest = true,
                // We don't support these features (yet)
                SupportsStepBack = false,
                SupportsSetVariable = false,
                SupportsRestartFrame = false,
                SupportsGotoTargetsRequest = false,
                SupportsStepInTargetsRequest = false,
                SupportsCompletionsRequest = false,
                SupportsModulesRequest = false,
                SupportsFunctionBreakpoints = false,
                SupportsConditionalBreakpoints = false,
                SupportsExceptionOptions = false,
                SupportsValueFormattingOptions = false,
                SupportsExceptionInfoRequest = false,
                SupportsDelayedStackTraceLoading = false,
                SupportsLoadedSourcesRequest = false,
                SupportsProgressReporting = false,
                SupportsRunInTerminalRequest = false,
                SupportsCancelRequest = false,
            };

            // Queue the initialized event to be sent after the response
            Task.Run(() =>
            {
                // Small delay to ensure response is sent first
                System.Threading.Thread.Sleep(50);
                _server?.SendEvent(new Event
                {
                    EventType = "initialized"
                });
                Trace.Info("Sent initialized event");
            });

            Trace.Info("Initialize request handled, capabilities sent");
            return CreateSuccessResponse(capabilities);
        }

        private Response HandleAttach(Request request)
        {
            Trace.Info("Attach request handled");
            return CreateSuccessResponse(null);
        }

        private Response HandleConfigurationDone(Request request)
        {
            lock (_stateLock)
            {
                _state = DapSessionState.Ready;
            }
            Trace.Info("Configuration done, debug session is ready");

            // Complete any pending wait for configuration
            return CreateSuccessResponse(null);
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

            return CreateSuccessResponse(null);
        }

        private Response HandleThreads(Request request)
        {
            // We have a single thread representing the job execution
            var body = new ThreadsResponseBody
            {
                Threads = new System.Collections.Generic.List<Thread>
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

            return CreateSuccessResponse(body);
        }

        private Response HandleStackTrace(Request request)
        {
            var args = request.Arguments?.ToObject<StackTraceArguments>();

            var frames = new System.Collections.Generic.List<StackFrame>();

            // Add current step as the top frame
            if (_currentStep != null)
            {
                frames.Add(new StackFrame
                {
                    Id = CurrentFrameId,
                    Name = _currentStep.DisplayName ?? "Current Step",
                    Line = 1,
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
                    Line = 1,
                    Column = 1,
                    PresentationHint = "subtle"
                });
            }

            // TODO: In Phase 2, add completed steps as additional frames

            var body = new StackTraceResponseBody
            {
                StackFrames = frames,
                TotalFrames = frames.Count
            };

            return CreateSuccessResponse(body);
        }

        private Response HandleScopes(Request request)
        {
            // Stub implementation - Phase 2 will populate with actual contexts
            var body = new ScopesResponseBody
            {
                Scopes = new System.Collections.Generic.List<Scope>
                {
                    new Scope { Name = "github", VariablesReference = 1, Expensive = false },
                    new Scope { Name = "env", VariablesReference = 2, Expensive = false },
                    new Scope { Name = "runner", VariablesReference = 3, Expensive = false },
                    new Scope { Name = "job", VariablesReference = 4, Expensive = false },
                    new Scope { Name = "steps", VariablesReference = 5, Expensive = false },
                    new Scope { Name = "secrets", VariablesReference = 6, Expensive = false, PresentationHint = "registers" },
                }
            };

            return CreateSuccessResponse(body);
        }

        private Response HandleVariables(Request request)
        {
            // Stub implementation - Phase 2 will populate with actual variable values
            var args = request.Arguments?.ToObject<VariablesArguments>();
            var variablesRef = args?.VariablesReference ?? 0;

            var body = new VariablesResponseBody
            {
                Variables = new System.Collections.Generic.List<Variable>
                {
                    new Variable
                    {
                        Name = "(stub)",
                        Value = $"Variables for scope {variablesRef} will be implemented in Phase 2",
                        Type = "string",
                        VariablesReference = 0
                    }
                }
            };

            return CreateSuccessResponse(body);
        }

        private Response HandleContinue(Request request)
        {
            Trace.Info("Continue command received");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                    _commandTcs?.TrySetResult(DapCommand.Continue);
                }
            }

            return CreateSuccessResponse(new ContinueResponseBody
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
                    _commandTcs?.TrySetResult(DapCommand.Next);
                }
            }

            return CreateSuccessResponse(null);
        }

        private Response HandlePause(Request request)
        {
            Trace.Info("Pause command received");

            // The runner will pause at the next step boundary
            lock (_stateLock)
            {
                // Just acknowledge - actual pause happens at step boundary
            }

            return CreateSuccessResponse(null);
        }

        private async Task<Response> HandleEvaluateAsync(Request request)
        {
            var args = request.Arguments?.ToObject<EvaluateArguments>();
            var expression = args?.Expression ?? "";
            var context = args?.Context ?? "hover";

            Trace.Info($"Evaluate: '{expression}' (context: {context})");

            // Stub implementation - Phase 4 will implement expression evaluation
            var body = new EvaluateResponseBody
            {
                Result = $"(evaluation of '{expression}' will be implemented in Phase 4)",
                Type = "string",
                VariablesReference = 0
            };

            return CreateSuccessResponse(body);
        }

        private Response HandleSetBreakpoints(Request request)
        {
            // Stub - breakpoints not implemented in demo
            Trace.Info("SetBreakpoints request (not implemented)");

            return CreateSuccessResponse(new
            {
                breakpoints = new object[0]
            });
        }

        private Response HandleSetExceptionBreakpoints(Request request)
        {
            // Stub - exception breakpoints not implemented in demo
            Trace.Info("SetExceptionBreakpoints request (not implemented)");

            return CreateSuccessResponse(new
            {
                breakpoints = new object[0]
            });
        }

        #endregion

        #region Step Coordination (called by StepsRunner)

        public async Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep)
        {
            if (!IsActive)
            {
                return;
            }

            _currentStep = step;
            _jobContext = jobContext;

            var reason = isFirstStep ? StopReason.Entry : StopReason.Step;
            var description = isFirstStep
                ? $"Stopped at job entry: {step.DisplayName}"
                : $"Stopped before step: {step.DisplayName}";

            Trace.Info($"Step starting: {step.DisplayName} (reason: {reason})");

            // Send stopped event to debugger
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

            // Wait for debugger command
            await WaitForCommandAsync();
        }

        public void OnStepCompleted(IStep step)
        {
            if (!IsActive)
            {
                return;
            }

            Trace.Info($"Step completed: {step.DisplayName}, result: {step.ExecutionContext?.Result}");

            // The step context will be available for inspection
            // Future: could pause here if "pause after step" is enabled
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

            // Send terminated event
            _server?.SendEvent(new Event
            {
                EventType = "terminated",
                Body = new TerminatedEventBody()
            });

            // Send exited event
            var exitCode = _jobContext?.Result == GitHub.DistributedTask.WebApi.TaskResult.Succeeded ? 0 : 1;
            _server?.SendEvent(new Event
            {
                EventType = "exited",
                Body = new ExitedEventBody
                {
                    ExitCode = exitCode
                }
            });
        }

        private async Task WaitForCommandAsync()
        {
            lock (_stateLock)
            {
                _state = DapSessionState.Paused;
                _commandTcs = new TaskCompletionSource<DapCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Trace.Info("Waiting for debugger command...");

            var command = await _commandTcs.Task;

            Trace.Info($"Received command: {command}");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                }
            }

            // Send continued event
            if (command == DapCommand.Continue || command == DapCommand.Next)
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

        #endregion

        #region Response Helpers

        private Response CreateSuccessResponse(object body)
        {
            return new Response
            {
                Success = true,
                Body = body
            };
        }

        private Response CreateErrorResponse(string message)
        {
            return new Response
            {
                Success = false,
                Message = message,
                Body = new ErrorResponseBody
                {
                    Error = new Message
                    {
                        Id = 1,
                        Format = message,
                        ShowUser = true
                    }
                }
            };
        }

        #endregion
    }
}
