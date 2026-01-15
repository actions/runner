# DAP Cancellation Support

**Status:** Implemented  
**Author:** OpenCode  
**Date:** January 2026

## Problem

When a cancellation signal for the current job comes in from the server, the DAP debugging session doesn't properly respond. If the runner is paused at a breakpoint waiting for debugger commands (or if a debugger never connects), the job gets stuck forever and requires manually deleting the runner.

### Root Cause

The `DapDebugSession.WaitForCommandAsync()` method uses a `TaskCompletionSource` that only completes when a DAP command arrives from the debugger. There's no mechanism to interrupt this wait when the job is cancelled externally.

Additionally, REPL shell commands use `CancellationToken.None`, so they also ignore job cancellation.

## Solution

Add proper cancellation token support throughout the DAP debugging flow:

1. Pass the job cancellation token to `OnStepStartingAsync` and `WaitForCommandAsync`
2. Register cancellation callbacks to release blocking waits
3. Add a `CancelSession()` method for external cancellation
4. Send DAP `terminated` and `exited` events to notify the debugger before cancelling
5. Use the cancellation token for REPL shell command execution

## Progress Checklist

- [x] **Phase 1:** Update IDapDebugSession interface
- [x] **Phase 2:** Update DapDebugSession implementation
- [x] **Phase 3:** Update StepsRunner to pass cancellation token
- [x] **Phase 4:** Update JobRunner to register cancellation handler
- [ ] **Phase 5:** Testing

## Files to Modify

| File | Changes |
|------|---------|
| `src/Runner.Worker/Dap/DapDebugSession.cs` | Add cancellation support to `OnStepStartingAsync`, `WaitForCommandAsync`, `ExecuteShellCommandAsync`, add `CancelSession` method |
| `src/Runner.Worker/StepsRunner.cs` | Pass `jobContext.CancellationToken` to `OnStepStartingAsync` |
| `src/Runner.Worker/JobRunner.cs` | Register cancellation callback to call `CancelSession` on the debug session |

## Detailed Implementation

### Phase 1: Update IDapDebugSession Interface

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs` (lines ~144-242)

Add new method to interface:

```csharp
/// <summary>
/// Cancels the debug session externally (e.g., job cancellation).
/// Sends terminated event to debugger and releases any blocking waits.
/// </summary>
void CancelSession();
```

Update existing method signature:

```csharp
// Change from:
Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep);

// Change to:
Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken);
```

### Phase 2: Update DapDebugSession Implementation

#### 2.1 Add cancellation token field

**Location:** Around line 260-300 (field declarations section)

```csharp
// Add field to store the job cancellation token for use by REPL commands
private CancellationToken _jobCancellationToken;
```

#### 2.2 Update OnStepStartingAsync

**Location:** Line 1159

```csharp
public async Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken)
{
    if (!IsActive)
    {
        return;
    }

    _currentStep = step;
    _jobContext = jobContext;
    _jobCancellationToken = cancellationToken; // Store for REPL commands

    // ... rest of existing implementation ...

    // Update the WaitForCommandAsync call at line 1212:
    await WaitForCommandAsync(cancellationToken);
}
```

#### 2.3 Update WaitForCommandAsync

**Location:** Line 1288

```csharp
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
```

#### 2.4 Add CancelSession method

**Location:** After `OnJobCompleted()` method, around line 1286

```csharp
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
```

#### 2.5 Update ExecuteShellCommandAsync

**Location:** Line 889-895

Change the `ExecuteAsync` call to use the stored cancellation token:

```csharp
int exitCode;
try
{
    exitCode = await processInvoker.ExecuteAsync(
        workingDirectory: workingDirectory,
        fileName: shell,
        arguments: string.Format(shellArgs, command),
        environment: env,
        requireExitCodeZero: false,
        cancellationToken: _jobCancellationToken);  // Changed from CancellationToken.None
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
```

### Phase 3: Update StepsRunner

**File:** `src/Runner.Worker/StepsRunner.cs`
**Location:** Line 204

Change:
```csharp
await debugSession.OnStepStartingAsync(step, jobContext, isFirstStep);
```

To:
```csharp
await debugSession.OnStepStartingAsync(step, jobContext, isFirstStep, jobContext.CancellationToken);
```

### Phase 4: Update JobRunner

**File:** `src/Runner.Worker/JobRunner.cs`

#### 4.1 Add cancellation registration

**Location:** After line 191 (after "Debugger connected" output), inside the debug mode block:

```csharp
// Register cancellation handler to properly terminate DAP session on job cancellation
CancellationTokenRegistration? dapCancellationRegistration = null;
try
{
    dapCancellationRegistration = jobRequestCancellationToken.Register(() =>
    {
        Trace.Info("Job cancelled - terminating DAP session");
        debugSession.CancelSession();
    });
}
catch (Exception ex)
{
    Trace.Warning($"Failed to register DAP cancellation handler: {ex.Message}");
}
```

Note: The `dapCancellationRegistration` variable should be declared at a higher scope (around line 116 with other declarations) so it can be disposed in the finally block.

#### 4.2 Dispose the registration

**Location:** In the finally block (after line 316, alongside dapServer cleanup):

```csharp
// Dispose DAP cancellation registration
dapCancellationRegistration?.Dispose();
```

## Behavior Summary

| Scenario | Before | After |
|----------|--------|-------|
| Paused at breakpoint, job cancelled | **Stuck forever** | DAP terminated event sent, wait released, job cancels normally |
| REPL command running, job cancelled | Command runs forever | Command cancelled, job cancels normally |
| Waiting for debugger connection, job cancelled | Already handled | No change (already works) |
| Debugger disconnects voluntarily | Works | No change |
| Normal step execution, job cancelled | Works | No change (existing cancellation logic handles this) |

## Exit Code Semantics

The `exited` event uses these exit codes:
- `0` = job succeeded
- `1` = job failed  
- `130` = job cancelled (standard Unix convention for SIGINT/Ctrl+C)

## Testing Scenarios

1. **Basic cancellation while paused:**
   - Start a debug job, let it pause at first step
   - Cancel the job from GitHub UI
   - Verify: DAP client receives terminated event, runner exits cleanly

2. **Cancellation during REPL command:**
   - Pause at a step, run `!sleep 60` in REPL
   - Cancel the job from GitHub UI
   - Verify: Sleep command terminates, DAP client receives terminated event, runner exits cleanly

3. **Cancellation before debugger connects:**
   - Start a debug job (it waits for connection)
   - Cancel the job before connecting a debugger
   - Verify: Runner exits cleanly (this already works, just verify no regression)

4. **Normal operation (no cancellation):**
   - Run through a debug session normally with step/continue
   - Verify: No change in behavior

5. **Debugger disconnect:**
   - Connect debugger, then disconnect it manually
   - Verify: Job continues to completion (existing behavior preserved)

## Estimated Effort

| Phase | Effort |
|-------|--------|
| Phase 1: Interface update | 15 min |
| Phase 2: DapDebugSession implementation | 45 min |
| Phase 3: StepsRunner update | 5 min |
| Phase 4: JobRunner update | 15 min |
| Phase 5: Testing | 30 min |
| **Total** | **~2 hours** |

## References

- DAP Specification: https://microsoft.github.io/debug-adapter-protocol/specification
- Related plan: `dap-debugging.md` (original DAP implementation)
