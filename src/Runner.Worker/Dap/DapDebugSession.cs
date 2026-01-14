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
    /// Stores information about a completed step for stack trace display.
    /// </summary>
    internal sealed class CompletedStepInfo
    {
        public string DisplayName { get; set; }
        public TaskResult? Result { get; set; }
        public int FrameId { get; set; }
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

        // Frame ID base for the current step (always 1)
        private const int CurrentFrameId = 1;

        // Frame IDs for completed steps start at 1000
        private const int CompletedFrameIdBase = 1000;

        private IDapServer _server;
        private DapSessionState _state = DapSessionState.WaitingForConnection;
        private InitializeRequestArguments _clientCapabilities;

        // Synchronization for step execution
        private TaskCompletionSource<DapCommand> _commandTcs;
        private readonly object _stateLock = new object();

        // Whether to pause before the next step (set by 'next' command)
        private bool _pauseOnNextStep = true;

        // Current execution context (set during OnStepStartingAsync)
        private IStep _currentStep;
        private IExecutionContext _jobContext;

        // Track completed steps for stack trace
        private readonly List<CompletedStepInfo> _completedSteps = new List<CompletedStepInfo>();
        private int _nextCompletedFrameId = CompletedFrameIdBase;

        // Variable provider for converting contexts to DAP variables
        private DapVariableProvider _variableProvider;

        public bool IsActive => _state == DapSessionState.Ready || _state == DapSessionState.Paused || _state == DapSessionState.Running;

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

            return CreateSuccessResponse(body);
        }

        private Response HandleStackTrace(Request request)
        {
            var args = request.Arguments?.ToObject<StackTraceArguments>();

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

            return CreateSuccessResponse(body);
        }

        private Response HandleScopes(Request request)
        {
            var args = request.Arguments?.ToObject<ScopesArguments>();
            var frameId = args?.FrameId ?? CurrentFrameId;

            // Get the execution context for the requested frame
            var context = GetExecutionContextForFrame(frameId);
            if (context == null)
            {
                // Return empty scopes if no context available
                return CreateSuccessResponse(new ScopesResponseBody { Scopes = new List<Scope>() });
            }

            // Use the variable provider to get scopes
            var scopes = _variableProvider.GetScopes(context, frameId);

            return CreateSuccessResponse(new ScopesResponseBody { Scopes = scopes });
        }

        private Response HandleVariables(Request request)
        {
            var args = request.Arguments?.ToObject<VariablesArguments>();
            var variablesRef = args?.VariablesReference ?? 0;

            // Get the current execution context
            var context = _currentStep?.ExecutionContext ?? _jobContext;
            if (context == null)
            {
                return CreateSuccessResponse(new VariablesResponseBody { Variables = new List<Variable>() });
            }

            // Use the variable provider to get variables
            var variables = _variableProvider.GetVariables(context, variablesRef);

            return CreateSuccessResponse(new VariablesResponseBody { Variables = variables });
        }

        private Response HandleContinue(Request request)
        {
            Trace.Info("Continue command received");

            lock (_stateLock)
            {
                if (_state == DapSessionState.Paused)
                {
                    _state = DapSessionState.Running;
                    _pauseOnNextStep = false; // Don't pause on next step
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
                    _pauseOnNextStep = true; // Pause before next step
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
                _pauseOnNextStep = true;
            }

            return CreateSuccessResponse(null);
        }

        private Task<Response> HandleEvaluateAsync(Request request)
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

            return Task.FromResult(CreateSuccessResponse(body));
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

            // Reset variable provider state for new step context
            _variableProvider.Reset();

            // Determine if we should pause
            bool shouldPause = isFirstStep || _pauseOnNextStep;

            if (!shouldPause)
            {
                Trace.Info($"Step starting (not pausing): {step.DisplayName}");
                return;
            }

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

            var result = step.ExecutionContext?.Result;
            Trace.Info($"Step completed: {step.DisplayName}, result: {result}");

            // Add to completed steps list
            _completedSteps.Add(new CompletedStepInfo
            {
                DisplayName = step.DisplayName,
                Result = result,
                FrameId = _nextCompletedFrameId++
            });

            // Clear current step reference since it's done
            // (will be set again when next step starts)
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

        /// <summary>
        /// Gets the execution context for a given frame ID.
        /// Currently only supports the current frame (completed steps don't have saved contexts).
        /// </summary>
        private IExecutionContext GetExecutionContextForFrame(int frameId)
        {
            if (frameId == CurrentFrameId)
            {
                return _currentStep?.ExecutionContext ?? _jobContext;
            }

            // For completed steps, we would need to save their execution contexts
            // For now, return null (variables won't be available for completed steps)
            return null;
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
