# DAP-Based Debugging for GitHub Actions Runner

**Status:** Draft  
**Author:** GitHub Actions Team  
**Date:** January 2026

## Progress Checklist

- [x] **Phase 1:** DAP Protocol Infrastructure (DapMessages.cs, DapServer.cs, basic DapDebugSession.cs)
- [x] **Phase 2:** Debug Session Logic (DapVariableProvider.cs, variable inspection, step history tracking)
- [x] **Phase 3:** StepsRunner Integration (pause hooks before/after step execution)
- [ ] **Phase 4:** Expression Evaluation & Shell (REPL)
- [ ] **Phase 5:** Startup Integration (JobRunner.cs modifications)

## Overview

This document describes the implementation of Debug Adapter Protocol (DAP) support in the GitHub Actions runner, enabling rich debugging of workflow jobs from any DAP-compatible editor (nvim-dap, VS Code, etc.).

## Goals

- **Primary:** Create a working demo to demonstrate the feasibility of DAP-based workflow debugging
- **Non-goal:** Production-ready, polished implementation (this is proof-of-concept)

## User Experience

1. User re-runs a failed job with "Enable debug logging" checked in GitHub UI
2. Runner (running locally) detects debug mode and starts DAP server on port 4711
3. Runner prints "Waiting for debugger on port 4711..." and pauses
4. User opens editor (nvim with nvim-dap), connects to debugger
5. Job execution begins, pausing before the first step
6. User can:
   - **Inspect variables:** View `github`, `env`, `inputs`, `steps`, `secrets` (redacted), `runner`, `job` contexts
   - **Evaluate expressions:** `${{ github.event.pull_request.title }}`
   - **Execute shell commands:** Run arbitrary commands in the job's environment (REPL)
   - **Step through job:** `next` moves to next step, `continue` runs to end
   - **Pause after steps:** Inspect step outputs before continuing

## Activation

DAP debugging activates automatically when the job is in debug mode:

- User enables "Enable debug logging" when re-running a job in GitHub UI
- Server sends `ACTIONS_STEP_DEBUG=true` in job variables
- Runner sets `Global.WriteDebug = true` and `runner.debug = "1"`
- DAP server starts on port 4711

**No additional configuration required.**

### Optional Configuration

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `ACTIONS_DAP_PORT` | `4711` | TCP port for DAP server (optional override) |

## Architecture

```
┌─────────────────────┐                    ┌─────────────────────────────────────────┐
│     nvim-dap        │                    │           Runner.Worker                 │
│   (DAP Client)      │◄───TCP:4711───────►│  ┌─────────────────────────────────┐   │
│                     │                    │  │     DapServer                    │   │
└─────────────────────┘                    │  │  - TCP listener                  │   │
                                           │  │  - DAP JSON protocol             │   │
                                           │  └──────────────┬──────────────────┘   │
                                           │                 │                       │
                                           │  ┌──────────────▼──────────────────┐   │
                                           │  │     DapDebugSession              │   │
                                           │  │  - Debug state management        │   │
                                           │  │  - Step coordination             │   │
                                           │  │  - Variable exposure             │   │
                                           │  │  - Expression evaluation         │   │
                                           │  │  - Shell execution (REPL)        │   │
                                           │  └──────────────┬──────────────────┘   │
                                           │                 │                       │
                                           │  ┌──────────────▼──────────────────┐   │
                                           │  │     StepsRunner (modified)       │   │
                                           │  │  - Pause before/after steps      │   │
                                           │  │  - Notify debug session          │   │
                                           │  └─────────────────────────────────┘   │
                                           └─────────────────────────────────────────┘
```

## DAP Concept Mapping

| DAP Concept | Actions Runner Equivalent |
|-------------|---------------------------|
| Thread | Single job execution |
| Stack Frame | Current step + completed steps (step history) |
| Scope | Context category: `github`, `env`, `inputs`, `steps`, `secrets`, `runner`, `job` |
| Variable | Individual context values |
| Breakpoint | Pause before specific step (future enhancement) |
| Step Over (Next) | Execute current step, pause before next |
| Continue | Run until job end |
| Evaluate | Evaluate `${{ }}` expressions OR execute shell commands (REPL) |

## File Structure

```
src/Runner.Worker/
├── Dap/
│   ├── DapServer.cs              # TCP listener, JSON protocol handling
│   ├── DapDebugSession.cs        # Debug state, step coordination  
│   ├── DapMessages.cs            # DAP protocol message types
│   └── DapVariableProvider.cs    # Converts ExecutionContext to DAP variables
```

## Implementation Phases

### Phase 1: DAP Protocol Infrastructure

#### 1.1 Protocol Messages (`Dap/DapMessages.cs`)

Base message types following DAP spec:

```csharp
public abstract class ProtocolMessage
{
    public int seq { get; set; }
    public string type { get; set; }  // "request", "response", "event"
}

public class Request : ProtocolMessage
{
    public string command { get; set; }
    public object arguments { get; set; }
}

public class Response : ProtocolMessage
{
    public int request_seq { get; set; }
    public bool success { get; set; }
    public string command { get; set; }
    public string message { get; set; }
    public object body { get; set; }
}

public class Event : ProtocolMessage
{
    public string @event { get; set; }
    public object body { get; set; }
}
```

Message framing: `Content-Length: N\r\n\r\n{json}`

#### 1.2 DAP Server (`Dap/DapServer.cs`)

```csharp
[ServiceLocator(Default = typeof(DapServer))]
public interface IDapServer : IRunnerService
{
    Task StartAsync(int port);
    Task WaitForConnectionAsync();
    Task StopAsync();
    void SendEvent(Event evt);
}

public sealed class DapServer : RunnerService, IDapServer
{
    private TcpListener _listener;
    private TcpClient _client;
    private IDapDebugSession _session;
    
    // TCP listener on configurable port
    // Single-client connection
    // Async read/write loop
    // Dispatch requests to DapDebugSession
}
```

### Phase 2: Debug Session Logic

#### 2.1 Debug Session (`Dap/DapDebugSession.cs`)

```csharp
public enum DapCommand { Continue, Next, Pause, Disconnect }
public enum PauseReason { Entry, Step, Breakpoint, Pause }

[ServiceLocator(Default = typeof(DapDebugSession))]
public interface IDapDebugSession : IRunnerService
{
    bool IsActive { get; }
    
    // Called by DapServer
    void Initialize(InitializeRequestArguments args);
    void Attach(AttachRequestArguments args);
    void ConfigurationDone();
    Task<DapCommand> WaitForCommandAsync();
    
    // Called by StepsRunner
    Task OnStepStartingAsync(IStep step, IExecutionContext jobContext);
    void OnStepCompleted(IStep step);
    
    // DAP requests
    ThreadsResponse GetThreads();
    StackTraceResponse GetStackTrace(int threadId);
    ScopesResponse GetScopes(int frameId);
    VariablesResponse GetVariables(int variablesReference);
    EvaluateResponse Evaluate(string expression, string context);
}

public sealed class DapDebugSession : RunnerService, IDapDebugSession
{
    private IExecutionContext _jobContext;
    private IStep _currentStep;
    private readonly List<IStep> _completedSteps = new();
    private TaskCompletionSource<DapCommand> _commandTcs;
    private bool _pauseAfterStep = false;
    
    // Object reference management for nested variables
    private int _nextVariableReference = 1;
    private readonly Dictionary<int, object> _variableReferences = new();
}
```

Core state machine:
1. **Waiting for client:** Server started, no client connected
2. **Initializing:** Client connected, exchanging capabilities
3. **Ready:** `configurationDone` received, waiting to start
4. **Paused (before step):** Stopped before step execution, waiting for command
5. **Running:** Executing a step
6. **Paused (after step):** Stopped after step execution, waiting for command

#### 2.2 Variable Provider (`Dap/DapVariableProvider.cs`)

Maps `ExecutionContext.ExpressionValues` to DAP scopes and variables:

| Scope | Source | Notes |
|-------|--------|-------|
| `github` | `ExpressionValues["github"]` | Full github context |
| `env` | `ExpressionValues["env"]` | Environment variables |
| `inputs` | `ExpressionValues["inputs"]` | Step inputs (when available) |
| `steps` | `Global.StepsContext.GetScope()` | Completed step outputs |
| `secrets` | `ExpressionValues["secrets"]` | Keys shown, values = `[REDACTED]` |
| `runner` | `ExpressionValues["runner"]` | Runner context |
| `job` | `ExpressionValues["job"]` | Job status |

Nested objects (e.g., `github.event.pull_request`) become expandable variables with child references.

### Phase 3: StepsRunner Integration

#### 3.1 Modify `StepsRunner.cs`

Add debug hooks at step boundaries:

```csharp
public async Task RunAsync(IExecutionContext jobContext)
{
    // Get debug session if available
    var debugSession = HostContext.TryGetService<IDapDebugSession>();
    bool isFirstStep = true;
    
    while (jobContext.JobSteps.Count > 0 || !checkPostJobActions)
    {
        // ... existing dequeue logic ...
        
        var step = jobContext.JobSteps.Dequeue();
        
        // Pause BEFORE step execution
        if (debugSession?.IsActive == true)
        {
            var reason = isFirstStep ? PauseReason.Entry : PauseReason.Step;
            await debugSession.OnStepStartingAsync(step, jobContext, reason);
            isFirstStep = false;
        }
        
        // ... existing step execution (condition eval, RunStepAsync, etc.) ...
        
        // Pause AFTER step execution (if requested)
        if (debugSession?.IsActive == true)
        {
            debugSession.OnStepCompleted(step);
            // Session may pause here to let user inspect outputs
        }
    }
}
```

### Phase 4: Expression Evaluation & Shell (REPL)

#### 4.1 Expression Evaluation

Reuse existing `PipelineTemplateEvaluator`:

```csharp
private string EvaluateExpression(string expression)
{
    // Strip ${{ }} wrapper if present
    var expr = expression.Trim();
    if (expr.StartsWith("${{") && expr.EndsWith("}}"))
    {
        expr = expr.Substring(3, expr.Length - 5).Trim();
    }
    
    var templateEvaluator = _currentStep.ExecutionContext.ToPipelineTemplateEvaluator();
    var token = new BasicExpressionToken(null, null, null, expr);
    
    var result = templateEvaluator.EvaluateStepDisplayName(
        token, 
        _currentStep.ExecutionContext.ExpressionValues, 
        _currentStep.ExecutionContext.ExpressionFunctions
    );
    
    return result;
}
```

#### 4.2 Shell Execution (REPL)

When `evaluate` is called with `context: "repl"`, spawn shell in step's environment:

```csharp
private async Task<EvaluateResponse> ExecuteShellCommand(string command)
{
    var processInvoker = HostContext.CreateService<IProcessInvoker>();
    var output = new StringBuilder();
    
    processInvoker.OutputDataReceived += (_, line) =>
    {
        output.AppendLine(line);
        // Stream to client in real-time
        _server.SendEvent(new OutputEvent
        {
            category = "stdout",
            output = line + "\n"
        });
    };
    
    processInvoker.ErrorDataReceived += (_, line) =>
    {
        _server.SendEvent(new OutputEvent
        {
            category = "stderr", 
            output = line + "\n"
        });
    };
    
    var env = BuildStepEnvironment(_currentStep);
    var workDir = _currentStep.ExecutionContext.GetGitHubContext("workspace");
    
    int exitCode = await processInvoker.ExecuteAsync(
        workingDirectory: workDir,
        fileName: GetDefaultShell(),  // /bin/bash on unix, pwsh/powershell on windows
        arguments: BuildShellArgs(command),
        environment: env,
        requireExitCodeZero: false,
        cancellationToken: CancellationToken.None
    );
    
    return new EvaluateResponse
    {
        result = output.ToString(),
        variablesReference = 0
    };
}
```

### Phase 5: Startup Integration

#### 5.1 Modify `JobRunner.cs`

Add DAP server startup after debug mode is detected (around line 159):

```csharp
if (jobContext.Global.WriteDebug)
{
    jobContext.SetRunnerContext("debug", "1");
    
    // Start DAP server for interactive debugging
    var dapServer = HostContext.GetService<IDapServer>();
    var port = int.Parse(
        Environment.GetEnvironmentVariable("ACTIONS_DAP_PORT") ?? "4711");
    
    await dapServer.StartAsync(port);
    Trace.Info($"DAP server listening on port {port}");
    jobContext.Output($"DAP debugger waiting for connection on port {port}...");
    
    // Block until debugger connects
    await dapServer.WaitForConnectionAsync();
    Trace.Info("DAP client connected, continuing job execution");
}
```

## DAP Capabilities

Capabilities to advertise in `InitializeResponse`:

```json
{
    "supportsConfigurationDoneRequest": true,
    "supportsEvaluateForHovers": true,
    "supportsTerminateDebuggee": true,
    "supportsStepBack": false,
    "supportsSetVariable": false,
    "supportsRestartFrame": false,
    "supportsGotoTargetsRequest": false,
    "supportsStepInTargetsRequest": false,
    "supportsCompletionsRequest": false,
    "supportsModulesRequest": false,
    "supportsExceptionOptions": false,
    "supportsValueFormattingOptions": false,
    "supportsExceptionInfoRequest": false,
    "supportsDelayedStackTraceLoading": false,
    "supportsLoadedSourcesRequest": false,
    "supportsProgressReporting": false,
    "supportsRunInTerminalRequest": false
}
```

## Client Configuration (nvim-dap)

Example configuration for nvim-dap:

```lua
local dap = require('dap')

dap.adapters.actions = {
    type = 'server',
    host = '127.0.0.1',
    port = 4711,
}

dap.configurations.yaml = {
    {
        type = 'actions',
        request = 'attach',
        name = 'Attach to Actions Runner',
    }
}
```

## Demo Flow

1. Trigger job re-run with "Enable debug logging" checked in GitHub UI
2. Runner starts, detects debug mode (`Global.WriteDebug == true`)
3. DAP server starts, console shows: `DAP debugger waiting for connection on port 4711...`
4. In nvim: `:lua require('dap').continue()`
5. Connection established, capabilities exchanged
6. Job begins, pauses before first step
7. nvim shows "stopped" state, variables panel shows contexts
8. User explores variables, evaluates expressions, runs shell commands
9. User presses `n` (next) to advance to next step
10. After step completes, user can inspect outputs before continuing
11. Repeat until job completes

## Testing Strategy

1. **Unit tests:** DAP protocol serialization, variable provider mapping
2. **Integration tests:** Mock DAP client verifying request/response sequences
3. **Manual testing:** Real job with nvim-dap attached

## Future Enhancements (Out of Scope for Demo)

- Composite action step-in (expand into sub-steps)
- Breakpoints on specific step names
- Watch expressions
- Conditional breakpoints
- Remote debugging (runner not on localhost)
- VS Code extension

## Estimated Effort

| Phase | Effort |
|-------|--------|
| Phase 1: Protocol Infrastructure | 4-6 hours |
| Phase 2: Debug Session Logic | 4-6 hours |
| Phase 3: StepsRunner Integration | 2-3 hours |
| Phase 4: Expression & Shell | 3-4 hours |
| Phase 5: Startup & Polish | 2-3 hours |
| **Total** | **~2-3 days** |

## Key Files to Modify

| File | Changes |
|------|---------|
| `src/Runner.Worker/JobRunner.cs` | Start DAP server when debug mode enabled |
| `src/Runner.Worker/StepsRunner.cs` | Add pause hooks before/after step execution |
| `src/Runner.Worker/Runner.Worker.csproj` | Add new Dap/ folder files |

## Key Files to Create

| File | Purpose |
|------|---------|
| `src/Runner.Worker/Dap/DapServer.cs` | TCP server, protocol framing |
| `src/Runner.Worker/Dap/DapDebugSession.cs` | Debug state machine, command handling |
| `src/Runner.Worker/Dap/DapMessages.cs` | Protocol message types |
| `src/Runner.Worker/Dap/DapVariableProvider.cs` | Context → DAP variable conversion |

## Reference Links

- [DAP Overview](https://microsoft.github.io/debug-adapter-protocol/overview)
- [DAP Specification](https://microsoft.github.io/debug-adapter-protocol/specification)
- [Enable Debug Logging (GitHub Docs)](https://docs.github.com/en/actions/how-tos/monitor-workflows/enable-debug-logging)
