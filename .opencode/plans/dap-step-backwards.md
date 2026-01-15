# DAP Step Backwards (Time-Travel Debugging)

**Status:** Implemented  
**Date:** January 2026  
**Related:** [dap-debugging.md](./dap-debugging.md)

## Overview

This document describes the implementation of step-backwards capability for the DAP debugging feature, enabling users to:

1. Pause at a job step (already implemented)
2. **Step backwards** to a previous step's checkpoint
3. Run REPL commands to modify state (env vars, files, etc.)
4. **Step forward** to re-run the step and see if changes fixed the issue

This is a form of "time-travel debugging" implemented via in-memory checkpoints.

## Progress Checklist

- [x] **Phase 1:** `StepCheckpoint` class and data structures
- [x] **Phase 2:** Checkpoint creation in `DapDebugSession`
- [x] **Phase 3:** Checkpoint restoration logic
- [x] **Phase 4:** DAP protocol handlers (`stepBack`, `reverseContinue`)
- [x] **Phase 5:** `StepsRunner` integration for step replay
- [x] **Phase 6:** Testing and edge cases

## Bug Fix History

### Bug: Step-back caused forward execution instead of backward (January 2026)

**Symptom:** Pressing step-back in nvim-dap caused the job to progress to the next step instead of going backward.

**Root Cause:** The `ConsumeRestoredCheckpoint()` method returned `_restoredCheckpoint` which was always `null` because it was only set *inside* `RestoreCheckpoint()`, but that method was never being called (blocked by the null checkpoint check in StepsRunner).

**Fix:**
1. Added `CheckpointIndex` property to `StepCheckpoint` class to track checkpoint position
2. Fixed `ConsumeRestoredCheckpoint()` to get checkpoint directly from `_checkpoints[_pendingRestoreCheckpoint.Value]` when a pending restore exists
3. Fixed `StepsRunner.cs` to use `checkpoint.CheckpointIndex` instead of `CheckpointCount - 1`

## Key Design Decision: Checkpoint Timing

**Checkpoints are created when the user commits to executing a step (presses next/continue), NOT when pausing.**

This approach has significant advantages:

1. **REPL changes automatically captured** - Any modifications made via REPL while paused become part of the checkpoint since the checkpoint is created after them.

2. **No special REPL tracking needed** - We don't need to detect `export` commands or track environment changes. Whatever state exists when the user steps forward is what gets checkpointed.

3. **Simple mental model** - "Checkpoint = exact state the step ran with"

4. **Handles step-back + modifications correctly:**
   ```
   Pause at step 3 (checkpoints [0], [1] exist from steps 1, 2)
   → REPL: export DEBUG=1 (modifies live state)
   → Step forward
   → Checkpoint [2] created NOW with DEBUG=1
   → Step 3 executes with DEBUG=1
   ```

## User Experience

### Example Flow

```
1. Job starts, step 1 (Checkout) about to run
   - User pauses (no checkpoints yet)
   - User presses "next"
   - Checkpoint [0] created with current state
   - Step 1 executes

2. Step 2 (Setup Node) about to run
   - User pauses
   - User presses "next"  
   - Checkpoint [1] created
   - Step 2 executes

3. Step 3 (Build) about to run
   - User pauses
   - User investigates via REPL:
     > !env | grep NODE
     NODE_ENV=production
   - User fixes via REPL:
     > !export NODE_ENV=development
   - Live state now has NODE_ENV=development
   
4. User presses "Step Back" (stepBack)
   - Restores to checkpoint [1] (before step 2)
   - NODE_ENV=development is LOST (restored to checkpoint state)
   - User is now paused before step 2

5. User presses "next" to re-run step 2
   - Checkpoint [1] is REPLACED with current state
   - Step 2 executes

6. Step 3 (Build) about to run again
   - User pauses
   - User sets env again: !export NODE_ENV=development
   - User presses "next"
   - Checkpoint [2] created WITH NODE_ENV=development
   - Step 3 executes with the fix applied!
```

### Key Insight

When you step back and then forward again, you get a chance to make modifications before each step runs. The checkpoint captures whatever state exists at the moment you commit to running the step.

### DAP Client Keybindings (nvim-dap example)

| Action | Default Key | DAP Request |
|--------|-------------|-------------|
| Step Back | (user configures) | `stepBack` |
| Reverse Continue | (user configures) | `reverseContinue` |

## Architecture

### Checkpoint Timing Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Step Execution Flow                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌──────────┐     ┌──────────────┐     ┌────────────────┐     ┌─────────┐ │
│   │  Pause   │────►│ User runs    │────►│ User presses   │────►│  Step   │ │
│   │  before  │     │ REPL cmds    │     │ next/continue  │     │ executes│ │
│   │  step    │     │ (optional)   │     │                │     │         │ │
│   └──────────┘     └──────────────┘     └───────┬────────┘     └─────────┘ │
│                                                  │                          │
│                                                  ▼                          │
│                                         ┌───────────────┐                   │
│                                         │  Checkpoint   │                   │
│                                         │  created HERE │                   │
│                                         │  (with any    │                   │
│                                         │  REPL changes)│                   │
│                                         └───────────────┘                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Checkpoint Storage

```
                                    ┌────────────────────────────────────────┐
                                    │          StepCheckpoint                │
                                    │  - StepIndex                           │
                                    │  - StepDisplayName                     │
                                    │  - EnvironmentVariables snapshot       │
                                    │  - EnvContextData snapshot             │
                                    │  - StepsContext snapshot               │
                                    │  - PrependPath snapshot                │
                                    │  - JobResult snapshot                  │
                                    │  - RemainingSteps (queue state)        │
                                    │  - CreatedAt timestamp                 │
                                    └────────────────────────────────────────┘
                                                      │
                                                      │ List<StepCheckpoint>
                                                      ▼
┌─────────────────────┐               ┌────────────────────────────────────────┐
│     nvim-dap        │               │         DapDebugSession                │
│   (DAP Client)      │◄─────────────►│  - _checkpoints: List<StepCheckpoint>  │
│                     │               │  - CreateCheckpoint()                  │
│  stepBack request   │──────────────►│  - RestoreCheckpoint()                 │
│  reverseContinue    │               │  - HandleStepBack()                    │
└─────────────────────┘               │  - HandleReverseContinue()             │
                                      └────────────────────────────────────────┘
                                                      │
                                                      ▼
                                      ┌────────────────────────────────────────┐
                                      │           StepsRunner                  │
                                      │  - Pauses before steps                 │
                                      │  - Creates checkpoint on next/continue │
                                      │  - Re-queues steps after restore       │
                                      └────────────────────────────────────────┘
```

### State Captured in Checkpoints

| State Category | Storage Location | Captured? | Notes |
|----------------|------------------|-----------|-------|
| **Environment Variables** | `Global.EnvironmentVariables` | Yes | Deep copied |
| **Env Context** | `ExpressionValues["env"]` | Yes | Deep copied |
| **Step Outputs** | `Global.StepsContext` | Yes | Per-step outputs |
| **Step Outcomes** | `Global.StepsContext` | Yes | outcome/conclusion |
| **PATH Additions** | `Global.PrependPath` | Yes | List copied |
| **Job Result** | `jobContext.Result` | Yes | Single value |
| **Job Status** | `JobContext.Status` | Yes | Single value |
| **Step Queue** | `jobContext.JobSteps` | Yes | Remaining steps |
| **Secret Masks** | `HostContext.SecretMasker` | No | Additive only |
| **Filesystem** | `_work/`, `_temp/` | No | Warning shown |
| **Container State** | Job container | No | Warning shown |

### Limitations

| Limitation | Impact | Mitigation |
|------------|--------|------------|
| Filesystem changes not reverted | Files created/modified by steps persist | Display warning to user |
| Secret masks accumulate | Once masked, stays masked | Acceptable behavior |
| Container internal state | State inside job container not reverted | Display warning if using container |
| Network calls | API calls, webhooks already sent | Document as limitation |
| Memory usage | Checkpoints consume memory | Limit to last N checkpoints (e.g., 50) |

## Implementation Details

### Phase 1: StepCheckpoint Class

**New File:** `src/Runner.Worker/Dap/StepCheckpoint.cs`

```csharp
using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Represents a snapshot of job state captured just before a step executes.
    /// Created when user issues next/continue command, after any REPL modifications.
    /// Used for step-back (time-travel) debugging.
    /// </summary>
    public sealed class StepCheckpoint
    {
        /// <summary>
        /// Zero-based index of the step in the job.
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// Display name of the step this checkpoint was created for.
        /// </summary>
        public string StepDisplayName { get; set; }

        /// <summary>
        /// Snapshot of Global.EnvironmentVariables.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Snapshot of ExpressionValues["env"] context data.
        /// </summary>
        public Dictionary<string, string> EnvContextData { get; set; }

        /// <summary>
        /// Snapshot of Global.PrependPath.
        /// </summary>
        public List<string> PrependPath { get; set; }

        /// <summary>
        /// Snapshot of job result.
        /// </summary>
        public TaskResult? JobResult { get; set; }

        /// <summary>
        /// Snapshot of job status.
        /// </summary>
        public ActionResult? JobStatus { get; set; }

        /// <summary>
        /// Snapshot of steps context (outputs, outcomes, conclusions).
        /// Key is "{scopeName}/{stepName}", value is the step's state.
        /// </summary>
        public Dictionary<string, StepStateSnapshot> StepsSnapshot { get; set; }

        /// <summary>
        /// The step that was about to execute (for re-running).
        /// </summary>
        public IStep CurrentStep { get; set; }

        /// <summary>
        /// Steps remaining in the queue after CurrentStep.
        /// </summary>
        public List<IStep> RemainingSteps { get; set; }

        /// <summary>
        /// When this checkpoint was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Snapshot of a single step's state in the steps context.
    /// </summary>
    public sealed class StepStateSnapshot
    {
        public ActionResult? Outcome { get; set; }
        public ActionResult? Conclusion { get; set; }
        public Dictionary<string, string> Outputs { get; set; }
    }
}
```

---

### Phase 2: Checkpoint Creation

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

Add checkpoint storage and creation method. Key point: `CreateCheckpoint()` is called when user issues next/continue, NOT when pausing.

```csharp
// Add to class fields
private readonly List<StepCheckpoint> _checkpoints = new List<StepCheckpoint>();
private const int MaxCheckpoints = 50;

// Track current step info for checkpoint creation (set during OnStepStartingAsync)
private IStep _pendingStep;
private List<IStep> _pendingRemainingSteps;
private int _pendingStepIndex;

/// <summary>
/// Gets the number of checkpoints available.
/// </summary>
public int CheckpointCount => _checkpoints.Count;

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
}

/// <summary>
/// Stores step info for later checkpoint creation.
/// Called at the start of OnStepStartingAsync, before pausing.
/// </summary>
private void SetPendingStepInfo(IStep step, IExecutionContext jobContext, int stepIndex, List<IStep> remainingSteps)
{
    _pendingStep = step;
    _pendingStepIndex = stepIndex;
    _pendingRemainingSteps = remainingSteps;
}

/// <summary>
/// Clears pending step info after step completes or is skipped.
/// </summary>
private void ClearPendingStepInfo()
{
    _pendingStep = null;
    _pendingRemainingSteps = null;
    _pendingStepIndex = 0;
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
```

---

### Phase 3: Checkpoint Restoration

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

```csharp
// Field to signal pending restoration to StepsRunner
private int? _pendingRestoreCheckpoint = null;
private StepCheckpoint _restoredCheckpoint = null;

/// <summary>
/// Gets whether a checkpoint restore is pending.
/// </summary>
public bool HasPendingRestore => _pendingRestoreCheckpoint.HasValue;

/// <summary>
/// Gets and clears the checkpoint that was just restored (for StepsRunner to use).
/// </summary>
public StepCheckpoint ConsumeRestoredCheckpoint()
{
    var checkpoint = _restoredCheckpoint;
    _restoredCheckpoint = null;
    _pendingRestoreCheckpoint = null;
    return checkpoint;
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

    // Restore steps context
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

private void RestoreEnvContext(IExecutionContext context, Dictionary<string, string> envData)
{
    if (context.ExpressionValues.TryGetValue("env", out var envContext))
    {
        if (envContext is DictionaryContextData dict)
        {
            dict.Clear();
            foreach (var kvp in envData)
            {
                dict[kvp.Key] = new StringContextData(kvp.Value);
            }
        }
        else if (envContext is CaseSensitiveDictionaryContextData csDict)
        {
            csDict.Clear();
            foreach (var kvp in envData)
            {
                csDict[kvp.Key] = new StringContextData(kvp.Value);
            }
        }
    }
}

private void RestoreStepsContext(StepsContext stepsContext, Dictionary<string, StepStateSnapshot> snapshot, string scopeName)
{
    // Note: StepsContext doesn't have a public Clear method, so we need to
    // work with its internal state. For now, we'll just ensure steps after
    // the checkpoint don't have their outputs visible.
    // 
    // A more complete implementation would require modifications to StepsContext
    // to support clearing/restoring state.
    
    Trace.Info($"Steps context restoration: {snapshot.Count} steps in snapshot");
    // TODO: Implement full StepsContext restoration when StepsContext supports it
}
```

---

### Phase 4: DAP Protocol Handlers

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

Update capabilities and add handlers:

```csharp
// In HandleInitialize, update capabilities:
var capabilities = new Capabilities
{
    // ... existing capabilities ...
    SupportsStepBack = true,  // NEW: Enable step back
    // ...
};

// Add new DapCommand enum values (update existing enum)
public enum DapCommand
{
    Continue,
    Next,
    Pause,
    Disconnect,
    StepBack,        // NEW
    ReverseContinue  // NEW
}

// In HandleRequestAsync switch, add new cases:
case "stepBack" => HandleStepBack(request),
case "reverseContinue" => HandleReverseContinue(request),

// New handlers:
private Response HandleStepBack(Request request)
{
    Trace.Info("StepBack command received");

    if (_checkpoints.Count == 0)
    {
        return CreateErrorResponse("No checkpoints available. Cannot step back before any steps have executed.");
    }

    // Step back to the most recent checkpoint
    // (which represents the state before the last executed step)
    int targetCheckpoint = _checkpoints.Count - 1;
    
    lock (_stateLock)
    {
        if (_state != DapSessionState.Paused)
        {
            return CreateErrorResponse("Can only step back when paused");
        }

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
        return CreateErrorResponse("No checkpoints available");
    }

    // Go back to the first checkpoint (beginning of job)
    lock (_stateLock)
    {
        if (_state != DapSessionState.Paused)
        {
            return CreateErrorResponse("Can only reverse continue when paused");
        }

        _pendingRestoreCheckpoint = 0;
        _state = DapSessionState.Running;
        _pauseOnNextStep = true;
        _commandTcs?.TrySetResult(DapCommand.ReverseContinue);
    }

    return CreateSuccessResponse(null);
}
```

**Update HandleContinue and HandleNext to create checkpoints:**

```csharp
private Response HandleContinue(Request request)
{
    Trace.Info("Continue command received");

    lock (_stateLock)
    {
        if (_state == DapSessionState.Paused)
        {
            _state = DapSessionState.Running;
            _pauseOnNextStep = false;
            _shouldCreateCheckpoint = true;  // NEW: Signal to create checkpoint
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
            _pauseOnNextStep = true;
            _shouldCreateCheckpoint = true;  // NEW: Signal to create checkpoint
            _commandTcs?.TrySetResult(DapCommand.Next);
        }
    }

    return CreateSuccessResponse(null);
}
```

**Add field and method to check/consume checkpoint flag:**

```csharp
// Add to class fields
private bool _shouldCreateCheckpoint = false;

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
```

---

### Phase 5: StepsRunner Integration

**File:** `src/Runner.Worker/StepsRunner.cs`

Modify to support checkpoint creation at the right time and step replay:

```csharp
public async Task RunAsync(IExecutionContext jobContext)
{
    // ... existing setup code ...

    var debugSession = HostContext.GetService<IDapDebugSession>();
    int stepIndex = 0;
    bool isFirstStep = true;

    while (jobContext.JobSteps.Count > 0 || !checkPostJobActions)
    {
        // ... existing post-job step handling ...

        var step = jobContext.JobSteps.Dequeue();
        
        // Capture remaining steps for potential checkpoint
        var remainingSteps = jobContext.JobSteps.ToList();

        Trace.Info($"Processing step: DisplayName='{step.DisplayName}'");
        // ... existing validation ...

        // Start
        step.ExecutionContext.Start();

        // ... existing expression functions setup ...

        if (!evaluateStepEnvFailed)
        {
            try
            {
                // ... existing cancellation registration ...

                // DAP: Pause BEFORE step execution, checkpoint created when user continues
                if (debugSession?.IsActive == true)
                {
                    // Store step info for checkpoint creation later
                    debugSession.SetPendingStepInfo(step, jobContext, stepIndex, remainingSteps);
                    
                    // Pause and wait for user command (next/continue/stepBack)
                    await debugSession.OnStepStartingAsync(step, jobContext, isFirstStep);
                    isFirstStep = false;

                    // Check if user requested to step back
                    if (debugSession.HasPendingRestore)
                    {
                        var checkpoint = debugSession.ConsumeRestoredCheckpoint();
                        if (checkpoint != null)
                        {
                            // Restore the checkpoint state
                            debugSession.RestoreCheckpoint(
                                debugSession.CheckpointCount - 1, 
                                jobContext);

                            // Re-queue the steps from checkpoint
                            while (jobContext.JobSteps.Count > 0)
                            {
                                jobContext.JobSteps.Dequeue();
                            }
                            
                            // Queue the checkpoint's step and remaining steps
                            jobContext.JobSteps.Enqueue(checkpoint.CurrentStep);
                            foreach (var remainingStep in checkpoint.RemainingSteps)
                            {
                                jobContext.JobSteps.Enqueue(remainingStep);
                            }

                            // Reset step index to checkpoint's index
                            stepIndex = checkpoint.StepIndex;
                            
                            // Clear pending step info since we're not executing this step
                            debugSession.ClearPendingStepInfo();
                            
                            // Skip to next iteration - will process restored step
                            continue;
                        }
                    }

                    // User pressed next/continue - create checkpoint NOW
                    // This captures any REPL modifications made while paused
                    if (debugSession.ShouldCreateCheckpoint())
                    {
                        debugSession.CreateCheckpointForPendingStep(jobContext);
                    }
                }

                // ... existing condition evaluation ...
                // ... existing step execution (RunStepAsync, CompleteStep) ...

            }
            finally
            {
                // ... existing cleanup ...
                
                // Clear pending step info after step completes
                debugSession?.ClearPendingStepInfo();
            }
        }

        // ... existing job result update ...

        // Notify DAP debugger AFTER step execution
        if (debugSession?.IsActive == true)
        {
            debugSession.OnStepCompleted(step);
        }

        stepIndex++;
        // ... existing logging ...
    }

    // ... existing job completion notification ...
}
```

---

### Phase 6: Testing

#### Unit Tests

```csharp
[Fact]
public void CreateCheckpointForPendingStep_CapturesCurrentState()
{
    // Arrange
    var session = new DapDebugSession();
    var jobContext = CreateMockJobContext();
    var mockStep = CreateMockStep("Test Step");
    
    jobContext.Global.EnvironmentVariables["ORIGINAL"] = "value";
    session.SetPendingStepInfo(mockStep, jobContext, 0, new List<IStep>());
    
    // Simulate REPL modification
    jobContext.Global.EnvironmentVariables["REPL_VAR"] = "repl_value";
    
    // Act
    session.CreateCheckpointForPendingStep(jobContext);
    
    // Assert
    Assert.Equal(1, session.CheckpointCount);
    // Checkpoint should have BOTH original and REPL-added vars
    var checkpoint = session.GetCheckpoints()[0];
    Assert.Equal("value", checkpoint.EnvironmentVariables["ORIGINAL"]);
    Assert.Equal("repl_value", checkpoint.EnvironmentVariables["REPL_VAR"]);
}

[Fact]
public void RestoreCheckpoint_RestoresEnvironmentVariables()
{
    // Arrange
    var session = new DapDebugSession();
    var jobContext = CreateMockJobContext();
    var mockStep = CreateMockStep("Step 1");
    
    jobContext.Global.EnvironmentVariables["VAR"] = "original";
    session.SetPendingStepInfo(mockStep, jobContext, 0, new List<IStep>());
    session.CreateCheckpointForPendingStep(jobContext);
    
    // Modify state after checkpoint
    jobContext.Global.EnvironmentVariables["VAR"] = "modified";
    jobContext.Global.EnvironmentVariables["NEW_VAR"] = "new";
    
    // Act
    session.RestoreCheckpoint(0, jobContext);
    
    // Assert
    Assert.Equal("original", jobContext.Global.EnvironmentVariables["VAR"]);
    Assert.False(jobContext.Global.EnvironmentVariables.ContainsKey("NEW_VAR"));
}

[Fact]
public void StepBack_RemovesSubsequentCheckpoints()
{
    // Arrange
    var session = new DapDebugSession();
    var jobContext = CreateMockJobContext();
    
    // Create 3 checkpoints
    for (int i = 0; i < 3; i++)
    {
        var step = CreateMockStep($"Step {i + 1}");
        session.SetPendingStepInfo(step, jobContext, i, new List<IStep>());
        session.CreateCheckpointForPendingStep(jobContext);
    }
    Assert.Equal(3, session.CheckpointCount);
    
    // Act - restore to checkpoint 1 (second checkpoint, index 1)
    session.RestoreCheckpoint(1, jobContext);
    
    // Assert - checkpoint 2 should be removed
    Assert.Equal(2, session.CheckpointCount);
}

[Fact]
public void HandleStepBack_FailsWhenNoCheckpoints()
{
    // Arrange
    var session = new DapDebugSession();
    session.SetState(DapSessionState.Paused);
    
    // Act
    var response = session.HandleStepBack(new Request { Command = "stepBack" });
    
    // Assert
    Assert.False(response.Success);
    Assert.Contains("No checkpoints available", response.Message);
}
```

#### Integration Test Scenarios

1. **Basic step-back:**
   - Execute steps 1, 2 (checkpoints [0], [1] created)
   - Pause before step 3
   - Step back
   - Verify: restored to state before step 2
   - Verify: only checkpoint [0] remains
   - Step forward, verify step 2 re-executes

2. **REPL modifications captured in checkpoint:**
   - Pause before step 1
   - REPL: `!export DEBUG=1`
   - Step forward (checkpoint [0] created with DEBUG=1)
   - Step 1 executes
   - Pause before step 2
   - Step back (restore checkpoint [0])
   - Verify: DEBUG=1 is in the restored state

3. **Multiple step-backs:**
   - Execute steps 1, 2, 3 (checkpoints [0], [1], [2])
   - Pause before step 4
   - Step back → now before step 3 (checkpoint [2] removed)
   - Step back → now before step 2 (checkpoint [1] removed)  
   - Only checkpoint [0] remains
   - Step forward → step 2 re-executes

4. **ReverseContinue to beginning:**
   - Execute steps 1, 2, 3
   - Pause before step 4
   - ReverseContinue → back to before step 1
   - Verify: all state reset to initial checkpoint [0]

5. **Modify state, step back, modify again, step forward:**
   - Pause before step 1
   - REPL: `!export VAR=first`
   - Step forward (checkpoint [0] has VAR=first)
   - Pause before step 2
   - Step back (restore checkpoint [0], VAR=first)
   - REPL: `!export VAR=second`
   - Step forward (checkpoint [0] REPLACED with VAR=second)
   - Verify: step 1 runs with VAR=second

#### Manual Testing Checklist

- [x] `stepBack` request works in nvim-dap
- [x] `reverseContinue` request works
- [x] Stack trace updates correctly after step-back
- [x] Variables panel shows restored state
- [x] REPL env changes are captured when stepping forward
- [x] Warning displayed about filesystem not being reverted
- [x] Error shown when trying to step back with no checkpoints
- [x] Checkpoint count stays within MaxCheckpoints limit

---

## Files Summary

### New Files

| File | Purpose |
|------|---------|
| `src/Runner.Worker/Dap/StepCheckpoint.cs` | Checkpoint data structures (includes `CheckpointIndex` for restore tracking) |

### Modified Files

| File | Changes |
|------|---------|
| `src/Runner.Worker/Dap/DapDebugSession.cs` | Checkpoint storage, creation, restoration, DAP handlers (`stepBack`, `reverseContinue`), `ConsumeRestoredCheckpoint()` fix |
| `src/Runner.Worker/StepsRunner.cs` | Checkpoint timing integration, step replay logic, uses `checkpoint.CheckpointIndex` for restoration |

---

## Sequence Diagram

```
User          nvim-dap         DapDebugSession        StepsRunner
 │               │                    │                    │
 │               │                    │    OnStepStarting  │
 │               │                    │◄───────────────────│
 │               │                    │  (stores pending   │
 │               │                    │   step info)       │
 │               │    stopped event   │                    │
 │               │◄───────────────────│                    │
 │               │                    │                    │
 │  (user runs REPL commands,        │                    │
 │   modifying live state)           │                    │
 │               │                    │                    │
 │   next        │                    │                    │
 │──────────────►│    next request    │                    │
 │               │───────────────────►│                    │
 │               │                    │  _shouldCreate     │
 │               │                    │  Checkpoint=true   │
 │               │                    │                    │
 │               │                    │  command=Next      │
 │               │                    │───────────────────►│
 │               │                    │                    │
 │               │                    │  ShouldCreate      │
 │               │                    │  Checkpoint()?     │
 │               │                    │◄───────────────────│
 │               │                    │     true           │
 │               │                    │───────────────────►│
 │               │                    │                    │
 │               │                    │  CreateCheckpoint  │
 │               │                    │  ForPendingStep()  │
 │               │                    │◄───────────────────│
 │               │                    │  (captures current │
 │               │                    │   state with REPL  │
 │               │                    │   modifications)   │
 │               │                    │                    │
 │               │                    │                    │  Step executes
 │               │                    │                    │──────────────►
```

---

## Estimated Effort

| Phase | Effort |
|-------|--------|
| Phase 1: StepCheckpoint class | 1 hour |
| Phase 2: Checkpoint creation | 2-3 hours |
| Phase 3: Checkpoint restoration | 2-3 hours |
| Phase 4: DAP protocol handlers | 1-2 hours |
| Phase 5: StepsRunner integration | 3-4 hours |
| Phase 6: Testing | 2-3 hours |
| **Total** | **~12-16 hours** |

---

## Future Enhancements (Out of Scope)

- Filesystem snapshotting via overlayfs
- Container state snapshotting
- Checkpoint persistence to disk (survive runner restart)
- Checkpoint browser UI in DAP client
- Conditional checkpoint creation (only on certain steps)
- Step to specific checkpoint by index (not just back/forward)
