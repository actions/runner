using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
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
        Disconnect,

        /// <summary>
        /// Step back to the previous checkpoint.
        /// </summary>
        StepBack,

        /// <summary>
        /// Reverse continue to the first checkpoint.
        /// </summary>
        ReverseContinue
    }

    /// <summary>
    /// Debug log levels for controlling verbosity of debug output.
    /// </summary>
    public enum DebugLogLevel
    {
        /// <summary>
        /// No debug logging.
        /// </summary>
        Off = 0,

        /// <summary>
        /// Errors and critical state changes only.
        /// </summary>
        Minimal = 1,

        /// <summary>
        /// Step lifecycle and checkpoint operations.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Everything including outputs, expressions, and StepsContext mutations.
        /// </summary>
        Verbose = 3
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
        /// Gets the number of checkpoints available for step-back.
        /// </summary>
        int CheckpointCount { get; }

        /// <summary>
        /// Gets whether a checkpoint restore is pending.
        /// </summary>
        bool HasPendingRestore { get; }

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
        /// <param name="cancellationToken">Cancellation token for job cancellation</param>
        /// <returns>Task that completes when execution should continue</returns>
        Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken);

        /// <summary>
        /// Called by StepsRunner after a step completes.
        /// </summary>
        /// <param name="step">The step that completed</param>
        void OnStepCompleted(IStep step);

        /// <summary>
        /// Notifies the session that the job has completed.
        /// </summary>
        void OnJobCompleted();

        /// <summary>
        /// Cancels the debug session externally (e.g., job cancellation).
        /// Sends terminated event to debugger and releases any blocking waits.
        /// </summary>
        void CancelSession();

        /// <summary>
        /// Stores step info for potential checkpoint creation.
        /// Called at the start of OnStepStartingAsync, before pausing.
        /// </summary>
        /// <param name="step">The step about to execute</param>
        /// <param name="jobContext">The job execution context</param>
        /// <param name="stepIndex">The zero-based index of the step</param>
        /// <param name="remainingSteps">Steps remaining in the queue after this step</param>
        void SetPendingStepInfo(IStep step, IExecutionContext jobContext, int stepIndex, List<IStep> remainingSteps);

        /// <summary>
        /// Clears pending step info after step completes or is skipped.
        /// </summary>
        void ClearPendingStepInfo();

        /// <summary>
        /// Checks and consumes the flag indicating a checkpoint should be created.
        /// Called by StepsRunner after WaitForCommandAsync returns.
        /// </summary>
        /// <returns>True if a checkpoint should be created</returns>
        bool ShouldCreateCheckpoint();

        /// <summary>
        /// Creates a checkpoint for the pending step, capturing current state.
        /// Called when user issues next/continue command, after any REPL modifications.
        /// </summary>
        /// <param name="jobContext">The job execution context</param>
        void CreateCheckpointForPendingStep(IExecutionContext jobContext);

        /// <summary>
        /// Gets and clears the checkpoint that was just restored (for StepsRunner to use).
        /// </summary>
        /// <returns>The restored checkpoint, or null if none</returns>
        StepCheckpoint ConsumeRestoredCheckpoint();

        /// <summary>
        /// Restores job state to a previous checkpoint.
        /// </summary>
        /// <param name="checkpointIndex">The index of the checkpoint to restore</param>
        /// <param name="jobContext">The job execution context</param>
        void RestoreCheckpoint(int checkpointIndex, IExecutionContext jobContext);
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

        // Checkpoint storage for step-back (time-travel) debugging
        private readonly List<StepCheckpoint> _checkpoints = new List<StepCheckpoint>();
        private const int MaxCheckpoints = 50;

        // Track pending step info for checkpoint creation (set during OnStepStartingAsync)
        private IStep _pendingStep;
        private List<IStep> _pendingRemainingSteps;
        private int _pendingStepIndex;

        // Flag to signal checkpoint creation when user continues
        private bool _shouldCreateCheckpoint = false;

        // Signal pending restoration to StepsRunner
        private int? _pendingRestoreCheckpoint = null;
        private StepCheckpoint _restoredCheckpoint = null;

        // Debug logging level (controlled via attach args or REPL command)
        private DebugLogLevel _debugLogLevel = DebugLogLevel.Off;

        // Job cancellation token for REPL commands and blocking waits
        private CancellationToken _jobCancellationToken;

        public bool IsActive => _state == DapSessionState.Ready || _state == DapSessionState.Paused || _state == DapSessionState.Running;

        public DapSessionState State => _state;

        public int CheckpointCount => _checkpoints.Count;

        public bool HasPendingRestore => _pendingRestoreCheckpoint.HasValue;

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
                    "stepBack" => HandleStepBack(request),
                    "reverseContinue" => HandleReverseContinue(request),
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
                // Step back (time-travel) debugging is supported
                SupportsStepBack = true,
                // We don't support these features (yet)
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

            // Parse debug logging from attach args
            if (request.Arguments != null)
            {
                // Check for debugLogging (boolean)
                var debugLogging = request.Arguments["debugLogging"];
                if (debugLogging != null && debugLogging.Type == Newtonsoft.Json.Linq.JTokenType.Boolean && (bool)debugLogging)
                {
                    _debugLogLevel = DebugLogLevel.Normal;
                    Trace.Info("Debug logging enabled via attach args (level: normal)");
                }

                // Check for debugLogLevel (string)
                var debugLogLevel = request.Arguments["debugLogLevel"];
                if (debugLogLevel != null && debugLogLevel.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    _debugLogLevel = ((string)debugLogLevel)?.ToLower() switch
                    {
                        "minimal" => DebugLogLevel.Minimal,
                        "normal" => DebugLogLevel.Normal,
                        "verbose" => DebugLogLevel.Verbose,
                        "off" => DebugLogLevel.Off,
                        _ => _debugLogLevel
                    };
                    Trace.Info($"Debug log level set via attach args: {_debugLogLevel}");
                }
            }

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
                    _shouldCreateCheckpoint = true; // Signal to create checkpoint before step executes
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
                    _shouldCreateCheckpoint = true; // Signal to create checkpoint before step executes
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

        private async Task<Response> HandleEvaluateAsync(Request request)
        {
            var args = request.Arguments?.ToObject<EvaluateArguments>();
            var expression = args?.Expression ?? "";
            var evalContext = args?.Context ?? "hover";

            Trace.Info($"Evaluate: '{expression}' (context: {evalContext})");

            // Check for !debug command (works in any context)
            if (expression.StartsWith("!debug", StringComparison.OrdinalIgnoreCase))
            {
                return HandleDebugCommand(expression);
            }

            // Get the current execution context
            var executionContext = _currentStep?.ExecutionContext ?? _jobContext;
            if (executionContext == null)
            {
                return CreateErrorResponse("No execution context available for evaluation");
            }

            try
            {
                // GitHub Actions expressions start with "${{" - always evaluate as expressions
                if (expression.StartsWith("${{"))
                {
                    var result = EvaluateExpression(expression, executionContext);
                    return CreateSuccessResponse(result);
                }

                // Check if this is a REPL/shell command:
                // - context is "repl" (from Debug Console pane)
                // - expression starts with "!" (explicit shell prefix for Watch pane)
                if (evalContext == "repl" || expression.StartsWith("!"))
                {
                    // Shell execution mode
                    var command = expression.TrimStart('!').Trim();
                    if (string.IsNullOrEmpty(command))
                    {
                        return CreateSuccessResponse(new EvaluateResponseBody
                        {
                            Result = "(empty command)",
                            Type = "string",
                            VariablesReference = 0
                        });
                    }

                    var result = await ExecuteShellCommandAsync(command, executionContext);
                    return CreateSuccessResponse(result);
                }
                else
                {
                    // Expression evaluation mode (Watch pane, hover, etc.)
                    var result = EvaluateExpression(expression, executionContext);
                    return CreateSuccessResponse(result);
                }
            }
            catch (Exception ex)
            {
                Trace.Error($"Evaluation failed: {ex}");
                return CreateErrorResponse($"Evaluation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluates a GitHub Actions expression (e.g., "github.event.pull_request.title" or "${{ github.ref }}")
        /// </summary>
        private EvaluateResponseBody EvaluateExpression(string expression, IExecutionContext context)
        {
            // Strip ${{ }} wrapper if present
            var expr = expression.Trim();
            if (expr.StartsWith("${{") && expr.EndsWith("}}"))
            {
                expr = expr.Substring(3, expr.Length - 5).Trim();
            }

            Trace.Info($"Evaluating expression: {expr}");

            // Create an expression token from the string
            var expressionToken = new BasicExpressionToken(fileId: null, line: null, column: null, expression: expr);

            // Get the template evaluator
            var templateEvaluator = context.ToPipelineTemplateEvaluator();

            // Evaluate using the display name evaluator which can handle arbitrary expressions
            string result;
            try
            {
                result = templateEvaluator.EvaluateStepDisplayName(
                    expressionToken,
                    context.ExpressionValues,
                    context.ExpressionFunctions);
            }
            catch (Exception ex)
            {
                // If the template evaluator fails, try direct expression evaluation
                Trace.Info($"Template evaluation failed, trying direct: {ex.Message}");
                result = EvaluateDirectExpression(expr, context);
            }

            // Mask secrets in the result
            result = HostContext.SecretMasker.MaskSecrets(result ?? "null");

            // Determine the type based on the result
            var type = DetermineResultType(result);

            return new EvaluateResponseBody
            {
                Result = result,
                Type = type,
                VariablesReference = 0
            };
        }

        /// <summary>
        /// Directly evaluates an expression by looking up values in the context data.
        /// Used as a fallback when the template evaluator doesn't work.
        /// </summary>
        private string EvaluateDirectExpression(string expression, IExecutionContext context)
        {
            // Try to look up the value directly in expression values
            var parts = expression.Split('.');

            if (parts.Length == 0 || !context.ExpressionValues.TryGetValue(parts[0], out var value))
            {
                return $"(unknown: {expression})";
            }

            // Navigate through nested properties
            object current = value;
            for (int i = 1; i < parts.Length && current != null; i++)
            {
                current = GetNestedValue(current, parts[i]);
            }

            if (current == null)
            {
                return "null";
            }

            // Convert to string representation
            if (current is PipelineContextData pcd)
            {
                return pcd.ToJToken()?.ToString() ?? "null";
            }

            return current.ToString();
        }

        /// <summary>
        /// Gets a nested value from a context data object.
        /// </summary>
        private object GetNestedValue(object data, string key)
        {
            switch (data)
            {
                case DictionaryContextData dict:
                    return dict.TryGetValue(key, out var dictValue) ? dictValue : null;

                case CaseSensitiveDictionaryContextData csDict:
                    return csDict.TryGetValue(key, out var csDictValue) ? csDictValue : null;

                case ArrayContextData array when int.TryParse(key.Trim('[', ']'), out var index):
                    return index >= 0 && index < array.Count ? array[index] : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Determines the type string for a result value.
        /// </summary>
        private string DetermineResultType(string value)
        {
            if (value == null || value == "null")
                return "null";
            if (value == "true" || value == "false")
                return "boolean";
            if (double.TryParse(value, out _))
                return "number";
            if (value.StartsWith("{") || value.StartsWith("["))
                return "object";
            return "string";
        }

        /// <summary>
        /// Executes a shell command in the job's environment and streams output to the debugger.
        /// </summary>
        private async Task<EvaluateResponseBody> ExecuteShellCommandAsync(string command, IExecutionContext context)
        {
            Trace.Info($"Executing shell command: {command}");

            var output = new StringBuilder();
            var errorOutput = new StringBuilder();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();

            processInvoker.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    output.AppendLine(args.Data);
                    // Stream output to the debugger in real-time (masked)
                    var maskedData = HostContext.SecretMasker.MaskSecrets(args.Data);
                    _server?.SendEvent(new Event
                    {
                        EventType = "output",
                        Body = new OutputEventBody
                        {
                            Category = "stdout",
                            Output = maskedData + "\n"
                        }
                    });
                }
            };

            processInvoker.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorOutput.AppendLine(args.Data);
                    // Stream error output to the debugger (masked)
                    var maskedData = HostContext.SecretMasker.MaskSecrets(args.Data);
                    _server?.SendEvent(new Event
                    {
                        EventType = "output",
                        Body = new OutputEventBody
                        {
                            Category = "stderr",
                            Output = maskedData + "\n"
                        }
                    });
                }
            };

            // Build the environment for the shell command
            var env = BuildShellEnvironment(context);

            // Get the working directory
            var workingDirectory = GetWorkingDirectory(context);

            // Get the default shell and arguments
            var (shell, shellArgs) = GetDefaultShell();

            Trace.Info($"Shell: {shell}, WorkDir: {workingDirectory}");

            int exitCode;
            try
            {
                exitCode = await processInvoker.ExecuteAsync(
                    workingDirectory: workingDirectory,
                    fileName: shell,
                    arguments: string.Format(shellArgs, command),
                    environment: env,
                    requireExitCodeZero: false,
                    cancellationToken: _jobCancellationToken);
            }
            catch (OperationCanceledException)
            {
                Trace.Info("Shell command cancelled due to job cancellation");
                return new EvaluateResponseBody
                {
                    Result = "(cancelled)",
                    Type = "error",
                    VariablesReference = 0
                };
            }
            catch (Exception ex)
            {
                Trace.Error($"Shell execution failed: {ex}");
                return new EvaluateResponseBody
                {
                    Result = $"Error: {ex.Message}",
                    Type = "error",
                    VariablesReference = 0
                };
            }

            // Return only exit code summary - output was already streamed to the debugger
            return new EvaluateResponseBody
            {
                Result = $"(exit code: {exitCode})",
                Type = exitCode == 0 ? "string" : "error",
                VariablesReference = 0
            };
        }

        /// <summary>
        /// Builds the environment dictionary for shell command execution.
        /// </summary>
        private IDictionary<string, string> BuildShellEnvironment(IExecutionContext context)
        {
            var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Copy current environment
            foreach (var entry in System.Environment.GetEnvironmentVariables())
            {
                if (entry is System.Collections.DictionaryEntry de)
                {
                    env[de.Key.ToString()] = de.Value?.ToString() ?? "";
                }
            }

            // Add context data as environment variables
            foreach (var contextEntry in context.ExpressionValues)
            {
                if (contextEntry.Value is IEnvironmentContextData runtimeContext)
                {
                    foreach (var envVar in runtimeContext.GetRuntimeEnvironmentVariables())
                    {
                        env[envVar.Key] = envVar.Value;
                    }
                }
            }

            // Add prepend path if available
            if (context.Global.PrependPath.Count > 0)
            {
                var prependPath = string.Join(Path.PathSeparator.ToString(), context.Global.PrependPath.Reverse<string>());
                if (env.TryGetValue("PATH", out var existingPath))
                {
                    env["PATH"] = $"{prependPath}{Path.PathSeparator}{existingPath}";
                }
                else
                {
                    env["PATH"] = prependPath;
                }
            }

            return env;
        }

        /// <summary>
        /// Gets the working directory for shell command execution.
        /// </summary>
        private string GetWorkingDirectory(IExecutionContext context)
        {
            // Try to get workspace from github context
            var githubContext = context.ExpressionValues["github"] as GitHubContext;
            if (githubContext != null)
            {
                var workspace = githubContext["workspace"] as StringContextData;
                if (workspace != null && Directory.Exists(workspace.Value))
                {
                    return workspace.Value;
                }
            }

            // Fallback to runner work directory
            var workDir = HostContext.GetDirectory(WellKnownDirectory.Work);
            if (Directory.Exists(workDir))
            {
                return workDir;
            }

            // Final fallback to current directory
            return System.Environment.CurrentDirectory;
        }

        /// <summary>
        /// Gets the default shell command and argument format for the current platform.
        /// </summary>
        private (string shell, string args) GetDefaultShell()
        {
#if OS_WINDOWS
            // Try pwsh first, then fall back to powershell
            var pwshPath = WhichUtil.Which("pwsh", false, Trace, null);
            if (!string.IsNullOrEmpty(pwshPath))
            {
                return (pwshPath, "-NoProfile -NonInteractive -Command \"{0}\"");
            }

            var psPath = WhichUtil.Which("powershell", false, Trace, null);
            if (!string.IsNullOrEmpty(psPath))
            {
                return (psPath, "-NoProfile -NonInteractive -Command \"{0}\"");
            }

            // Fallback to cmd
            return ("cmd.exe", "/C \"{0}\"");
#else
            // Try bash first, then sh
            var bashPath = WhichUtil.Which("bash", false, Trace, null);
            if (!string.IsNullOrEmpty(bashPath))
            {
                return (bashPath, "-c \"{0}\"");
            }

            var shPath = WhichUtil.Which("sh", false, Trace, null);
            if (!string.IsNullOrEmpty(shPath))
            {
                return (shPath, "-c \"{0}\"");
            }

            // Fallback
            return ("/bin/sh", "-c \"{0}\"");
#endif
        }

        /// <summary>
        /// Handles the !debug REPL command for controlling debug logging.
        /// </summary>
        private Response HandleDebugCommand(string command)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var arg = parts.Length > 1 ? parts[1].ToLower() : "status";

            string result;
            switch (arg)
            {
                case "on":
                    _debugLogLevel = DebugLogLevel.Normal;
                    result = "Debug logging enabled (level: normal)";
                    Trace.Info("Debug logging enabled via REPL command");
                    break;
                case "off":
                    _debugLogLevel = DebugLogLevel.Off;
                    result = "Debug logging disabled";
                    Trace.Info("Debug logging disabled via REPL command");
                    break;
                case "minimal":
                    _debugLogLevel = DebugLogLevel.Minimal;
                    result = "Debug logging set to minimal";
                    Trace.Info("Debug log level set to minimal via REPL command");
                    break;
                case "normal":
                    _debugLogLevel = DebugLogLevel.Normal;
                    result = "Debug logging set to normal";
                    Trace.Info("Debug log level set to normal via REPL command");
                    break;
                case "verbose":
                    _debugLogLevel = DebugLogLevel.Verbose;
                    result = "Debug logging set to verbose";
                    Trace.Info("Debug log level set to verbose via REPL command");
                    break;
                case "status":
                default:
                    result = $"Debug logging: {_debugLogLevel}\n" +
                             $"Commands: !debug [on|off|minimal|normal|verbose|status]";
                    break;
            }

            return CreateSuccessResponse(new EvaluateResponseBody
            {
                Result = result,
                VariablesReference = 0
            });
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

        private Response HandleStepBack(Request request)
        {
            Trace.Info("StepBack command received");

            if (_checkpoints.Count == 0)
            {
                return CreateErrorResponse("No checkpoints available. Cannot step back before any steps have executed.");
            }

            lock (_stateLock)
            {
                if (_state != DapSessionState.Paused)
                {
                    return CreateErrorResponse("Can only step back when paused");
                }

                // Step back to the most recent checkpoint
                // (which represents the state before the last executed step)
                int targetCheckpoint = _checkpoints.Count - 1;
                _pendingRestoreCheckpoint = targetCheckpoint;
                _state = DapSessionState.Running;
                _pauseOnNextStep = true; // Pause immediately after restore
                _commandTcs?.TrySetResult(DapCommand.StepBack);
            }

            return CreateSuccessResponse(null);
        }

        private Response HandleReverseContinue(Request request)
        {
            Trace.Info("ReverseContinue command received");

            if (_checkpoints.Count == 0)
            {
                return CreateErrorResponse("No checkpoints available. Cannot reverse continue before any steps have executed.");
            }

            lock (_stateLock)
            {
                if (_state != DapSessionState.Paused)
                {
                    return CreateErrorResponse("Can only reverse continue when paused");
                }

                // Go back to the first checkpoint (beginning of job)
                _pendingRestoreCheckpoint = 0;
                _state = DapSessionState.Running;
                _pauseOnNextStep = true;
                _commandTcs?.TrySetResult(DapCommand.ReverseContinue);
            }

            return CreateSuccessResponse(null);
        }

        #endregion

        #region Step Coordination (called by StepsRunner)

        public async Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken)
        {
            if (!IsActive)
            {
                return;
            }

            _currentStep = step;
            _jobContext = jobContext;
            _jobCancellationToken = cancellationToken; // Store for REPL commands

            // Hook up StepsContext debug logging (do this once when we first get jobContext)
            if (jobContext.Global.StepsContext.OnDebugLog == null)
            {
                jobContext.Global.StepsContext.OnDebugLog = (msg) => DebugLog(msg, DebugLogLevel.Verbose);
            }

            // Reset variable provider state for new step context
            _variableProvider.Reset();

            // Log step starting
            DebugLog($"[Step] Starting: '{step.DisplayName}' (index={_pendingStepIndex})");
            DebugLog($"[Step] Checkpoints available: {_checkpoints.Count}");

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

            // Log step completion
            DebugLog($"[Step] Completed: '{step.DisplayName}', result={result}");

            // Log current steps context state for this step at Normal level
            if (_debugLogLevel >= DebugLogLevel.Normal)
            {
                var stepsScope = step.ExecutionContext?.Global?.StepsContext?.GetScope(step.ExecutionContext.ScopeName);
                if (stepsScope != null && !string.IsNullOrEmpty(step.ExecutionContext?.ContextName))
                {
                    if (stepsScope.TryGetValue(step.ExecutionContext.ContextName, out var stepData) && stepData is DictionaryContextData sd)
                    {
                        var outcome = sd.TryGetValue("outcome", out var o) && o is StringContextData os ? os.Value : "null";
                        var conclusion = sd.TryGetValue("conclusion", out var c) && c is StringContextData cs ? cs.Value : "null";
                        DebugLog($"[Step] Context state: outcome={outcome}, conclusion={conclusion}");
                    }
                }
            }

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

        /// <summary>
        /// Cancels the debug session externally (e.g., job cancellation).
        /// Sends terminated/exited events to debugger and releases any blocking waits.
        /// </summary>
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

            Trace.Info("Debug session cancelled");
        }

        private async Task WaitForCommandAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                _state = DapSessionState.Paused;
                _commandTcs = new TaskCompletionSource<DapCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Trace.Info("Waiting for debugger command...");

            // Register cancellation to release the wait
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

                // Send continued event (only for normal commands, not cancellation)
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

        #region Checkpoint Methods (Time-Travel Debugging)

        /// <summary>
        /// Stores step info for potential checkpoint creation.
        /// Called at the start of step processing, before pausing.
        /// </summary>
        public void SetPendingStepInfo(IStep step, IExecutionContext jobContext, int stepIndex, List<IStep> remainingSteps)
        {
            _pendingStep = step;
            _pendingStepIndex = stepIndex;
            _pendingRemainingSteps = remainingSteps;
            Trace.Info($"Pending step info set: '{step.DisplayName}' (index {stepIndex}, {remainingSteps.Count} remaining)");
        }

        /// <summary>
        /// Clears pending step info after step completes or is skipped.
        /// </summary>
        public void ClearPendingStepInfo()
        {
            _pendingStep = null;
            _pendingRemainingSteps = null;
            _pendingStepIndex = 0;
        }

        /// <summary>
        /// Checks and consumes the flag indicating a checkpoint should be created.
        /// Called by StepsRunner after WaitForCommandAsync returns.
        /// </summary>
        public bool ShouldCreateCheckpoint()
        {
            var should = _shouldCreateCheckpoint;
            _shouldCreateCheckpoint = false;
            return should;
        }

        /// <summary>
        /// Called when user issues next/continue command.
        /// Creates checkpoint capturing current state (including any REPL modifications).
        /// </summary>
        public void CreateCheckpointForPendingStep(IExecutionContext jobContext)
        {
            if (_pendingStep == null)
            {
                Trace.Warning("CreateCheckpointForPendingStep called but no pending step");
                return;
            }

            // Enforce maximum checkpoint limit
            if (_checkpoints.Count >= MaxCheckpoints)
            {
                _checkpoints.RemoveAt(0); // Remove oldest
                Trace.Info($"Removed oldest checkpoint (exceeded max {MaxCheckpoints})");
            }

            var checkpoint = new StepCheckpoint
            {
                StepIndex = _pendingStepIndex,
                StepDisplayName = _pendingStep.DisplayName,
                CreatedAt = DateTime.UtcNow,
                CurrentStep = _pendingStep,
                RemainingSteps = new List<IStep>(_pendingRemainingSteps),

                // Deep copy environment variables - captures any REPL modifications
                EnvironmentVariables = new Dictionary<string, string>(
                    jobContext.Global.EnvironmentVariables,
                    StringComparer.OrdinalIgnoreCase),

                // Deep copy env context
                EnvContextData = CopyEnvContextData(jobContext),

                // Copy prepend path
                PrependPath = new List<string>(jobContext.Global.PrependPath),

                // Copy job state
                JobResult = jobContext.Result,
                JobStatus = jobContext.JobContext.Status,

                // Snapshot steps context
                StepsSnapshot = SnapshotStepsContext(jobContext.Global.StepsContext, jobContext.ScopeName)
            };

            _checkpoints.Add(checkpoint);
            Trace.Info($"Created checkpoint [{_checkpoints.Count - 1}] for step '{_pendingStep.DisplayName}' " +
                       $"(env vars: {checkpoint.EnvironmentVariables.Count}, " +
                       $"prepend paths: {checkpoint.PrependPath.Count})");

            // Debug logging for checkpoint creation
            DebugLog($"[Checkpoint] Created [{_checkpoints.Count - 1}] for step '{_pendingStep.DisplayName}'");
            if (_debugLogLevel >= DebugLogLevel.Verbose)
            {
                DebugLog($"[Checkpoint] Snapshot contains {checkpoint.StepsSnapshot.Count} step(s)", DebugLogLevel.Verbose);
                foreach (var entry in checkpoint.StepsSnapshot)
                {
                    DebugLog($"[Checkpoint]   {entry.Key}: outcome={entry.Value.Outcome}, conclusion={entry.Value.Conclusion}", DebugLogLevel.Verbose);
                }
            }

            // Send notification to debugger
            _server?.SendEvent(new Event
            {
                EventType = "output",
                Body = new OutputEventBody
                {
                    Category = "console",
                    Output = $"Checkpoint [{_checkpoints.Count - 1}] created for step '{_pendingStep.DisplayName}'\n"
                }
            });
        }

        /// <summary>
        /// Gets and clears the checkpoint that should be restored (for StepsRunner to use).
        /// Returns the checkpoint from the pending restore index if set.
        /// The returned checkpoint's CheckpointIndex property is set for use with RestoreCheckpoint().
        /// </summary>
        public StepCheckpoint ConsumeRestoredCheckpoint()
        {
            // If there's a pending restore, get the checkpoint from the index
            // (This is the correct checkpoint to restore to)
            if (_pendingRestoreCheckpoint.HasValue)
            {
                var checkpointIndex = _pendingRestoreCheckpoint.Value;
                if (checkpointIndex >= 0 && checkpointIndex < _checkpoints.Count)
                {
                    var checkpoint = _checkpoints[checkpointIndex];
                    // Ensure the checkpoint knows its own index (for RestoreCheckpoint call)
                    checkpoint.CheckpointIndex = checkpointIndex;
                    // Clear the pending state - the caller will handle restoration
                    _pendingRestoreCheckpoint = null;
                    _restoredCheckpoint = null;
                    return checkpoint;
                }
            }

            // Fallback to already-restored checkpoint (shouldn't normally be used)
            var restoredCheckpoint = _restoredCheckpoint;
            _restoredCheckpoint = null;
            _pendingRestoreCheckpoint = null;
            return restoredCheckpoint;
        }

        /// <summary>
        /// Restores job state to a previous checkpoint.
        /// </summary>
        public void RestoreCheckpoint(int checkpointIndex, IExecutionContext jobContext)
        {
            if (checkpointIndex < 0 || checkpointIndex >= _checkpoints.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex),
                    $"Checkpoint index {checkpointIndex} out of range (0-{_checkpoints.Count - 1})");
            }

            var checkpoint = _checkpoints[checkpointIndex];
            Trace.Info($"Restoring checkpoint [{checkpointIndex}] for step '{checkpoint.StepDisplayName}'");

            // Debug logging for checkpoint restoration
            DebugLog($"[Checkpoint] Restoring [{checkpointIndex}] for step '{checkpoint.StepDisplayName}'");
            if (_debugLogLevel >= DebugLogLevel.Verbose)
            {
                DebugLog($"[Checkpoint] Snapshot has {checkpoint.StepsSnapshot.Count} step(s)", DebugLogLevel.Verbose);
            }

            // Restore environment variables
            jobContext.Global.EnvironmentVariables.Clear();
            foreach (var kvp in checkpoint.EnvironmentVariables)
            {
                jobContext.Global.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            // Restore env context
            RestoreEnvContext(jobContext, checkpoint.EnvContextData);

            // Restore prepend path
            jobContext.Global.PrependPath.Clear();
            jobContext.Global.PrependPath.AddRange(checkpoint.PrependPath);

            // Restore job result
            jobContext.Result = checkpoint.JobResult;
            jobContext.JobContext.Status = checkpoint.JobStatus;

            // Restore steps context - clear scope and restore from snapshot
            RestoreStepsContext(jobContext.Global.StepsContext, checkpoint.StepsSnapshot, jobContext.ScopeName);

            // Clear checkpoints after this one (they're now invalid)
            if (checkpointIndex + 1 < _checkpoints.Count)
            {
                var removeCount = _checkpoints.Count - checkpointIndex - 1;
                _checkpoints.RemoveRange(checkpointIndex + 1, removeCount);
                Trace.Info($"Removed {removeCount} invalidated checkpoints");
            }

            // Clear completed steps list for frames after this checkpoint
            while (_completedSteps.Count > checkpointIndex)
            {
                _completedSteps.RemoveAt(_completedSteps.Count - 1);
            }

            // Store restored checkpoint for StepsRunner to consume
            _restoredCheckpoint = checkpoint;

            // Send notification to debugger
            _server?.SendEvent(new Event
            {
                EventType = "output",
                Body = new OutputEventBody
                {
                    Category = "console",
                    Output = $"Restored to checkpoint [{checkpointIndex}] before step '{checkpoint.StepDisplayName}'\n" +
                             $"Note: Filesystem changes were NOT reverted\n"
                }
            });

            Trace.Info($"Checkpoint restored. {_checkpoints.Count} checkpoints remain.");
        }

        private Dictionary<string, string> CopyEnvContextData(IExecutionContext context)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (context.ExpressionValues.TryGetValue("env", out var envData))
            {
                if (envData is DictionaryContextData dict)
                {
                    foreach (var kvp in dict)
                    {
                        if (kvp.Value is StringContextData strData)
                        {
                            result[kvp.Key] = strData.Value;
                        }
                    }
                }
                else if (envData is CaseSensitiveDictionaryContextData csDict)
                {
                    foreach (var kvp in csDict)
                    {
                        if (kvp.Value is StringContextData strData)
                        {
                            result[kvp.Key] = strData.Value;
                        }
                    }
                }
            }

            return result;
        }

        private void RestoreEnvContext(IExecutionContext context, Dictionary<string, string> envData)
        {
            // Create a new env context with restored data and replace the old one
            // Since DictionaryContextData doesn't have a Clear method, we replace the entire object
#if OS_WINDOWS
            var newEnvContext = new DictionaryContextData();
#else
            var newEnvContext = new CaseSensitiveDictionaryContextData();
#endif
            foreach (var kvp in envData)
            {
                newEnvContext[kvp.Key] = new StringContextData(kvp.Value);
            }

            context.ExpressionValues["env"] = newEnvContext;
        }

        private void RestoreStepsContext(StepsContext stepsContext, Dictionary<string, StepStateSnapshot> snapshot, string scopeName)
        {
            // Normalize scope name (null -> empty string)
            scopeName = scopeName ?? string.Empty;

            DebugLog($"[StepsContext] Restoring: clearing scope '{(string.IsNullOrEmpty(scopeName) ? "(root)" : scopeName)}', will restore {snapshot.Count} step(s)");

            // Clear the entire scope - removes all step data that shouldn't exist yet in this timeline
            stepsContext.ClearScope(scopeName);

            // Restore each step's state from the snapshot
            foreach (var entry in snapshot)
            {
                // Key format is "{scopeName}/{stepName}" - e.g., "/thefoo" for root-level steps (empty scope)
                var key = entry.Key;
                var slashIndex = key.IndexOf('/');

                if (slashIndex >= 0)
                {
                    var snapshotScopeName = slashIndex > 0 ? key.Substring(0, slashIndex) : string.Empty;
                    var stepName = key.Substring(slashIndex + 1);

                    // Only restore steps for the current scope
                    if (snapshotScopeName == scopeName)
                    {
                        var state = entry.Value;

                        if (state.Outcome.HasValue)
                        {
                            stepsContext.SetOutcome(scopeName, stepName, state.Outcome.Value);
                        }
                        if (state.Conclusion.HasValue)
                        {
                            stepsContext.SetConclusion(scopeName, stepName, state.Conclusion.Value);
                        }

                        // Restore outputs
                        if (state.Outputs != null)
                        {
                            foreach (var output in state.Outputs)
                            {
                                stepsContext.SetOutput(scopeName, stepName, output.Key, output.Value, out _);
                            }
                        }

                        DebugLog($"[StepsContext] Restored: step='{stepName}', outcome={state.Outcome}, conclusion={state.Conclusion}", DebugLogLevel.Verbose);
                    }
                }
            }

            Trace.Info($"Steps context restored: cleared scope '{scopeName}' and restored {snapshot.Count} step(s) from snapshot");
        }

        private Dictionary<string, StepStateSnapshot> SnapshotStepsContext(StepsContext stepsContext, string scopeName)
        {
            var result = new Dictionary<string, StepStateSnapshot>();

            // Get the scope's context data
            var scopeData = stepsContext.GetScope(scopeName);
            if (scopeData != null)
            {
                foreach (var stepEntry in scopeData)
                {
                    if (stepEntry.Value is DictionaryContextData stepData)
                    {
                        var snapshot = new StepStateSnapshot
                        {
                            Outputs = new Dictionary<string, string>()
                        };

                        // Extract outcome
                        if (stepData.TryGetValue("outcome", out var outcome) && outcome is StringContextData outcomeStr)
                        {
                            snapshot.Outcome = ParseActionResult(outcomeStr.Value);
                        }

                        // Extract conclusion
                        if (stepData.TryGetValue("conclusion", out var conclusion) && conclusion is StringContextData conclusionStr)
                        {
                            snapshot.Conclusion = ParseActionResult(conclusionStr.Value);
                        }

                        // Extract outputs
                        if (stepData.TryGetValue("outputs", out var outputs) && outputs is DictionaryContextData outputsDict)
                        {
                            foreach (var output in outputsDict)
                            {
                                if (output.Value is StringContextData outputStr)
                                {
                                    snapshot.Outputs[output.Key] = outputStr.Value;
                                }
                            }
                        }

                        result[$"{scopeName}/{stepEntry.Key}"] = snapshot;
                    }
                }
            }

            return result;
        }

        private ActionResult? ParseActionResult(string value)
        {
            return value?.ToLower() switch
            {
                "success" => ActionResult.Success,
                "failure" => ActionResult.Failure,
                "cancelled" => ActionResult.Cancelled,
                "skipped" => ActionResult.Skipped,
                _ => null
            };
        }

        #endregion

        #region Debug Logging

        /// <summary>
        /// Sends a debug log message to the DAP console if the current log level permits.
        /// </summary>
        /// <param name="message">The message to log (will be prefixed with [DEBUG])</param>
        /// <param name="minLevel">Minimum level required for this message to be logged</param>
        private void DebugLog(string message, DebugLogLevel minLevel = DebugLogLevel.Normal)
        {
            if (_debugLogLevel >= minLevel)
            {
                _server?.SendEvent(new Event
                {
                    EventType = "output",
                    Body = new OutputEventBody
                    {
                        Category = "console",
                        Output = $"[DEBUG] {message}\n"
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
