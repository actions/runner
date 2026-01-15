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

        private async Task<Response> HandleEvaluateAsync(Request request)
        {
            var args = request.Arguments?.ToObject<EvaluateArguments>();
            var expression = args?.Expression ?? "";
            var evalContext = args?.Context ?? "hover";

            Trace.Info($"Evaluate: '{expression}' (context: {evalContext})");

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
                    cancellationToken: CancellationToken.None);
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
