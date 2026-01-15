# DAP Debug Logging Feature

**Status:** Implemented  
**Date:** January 2026  
**Related:** [dap-debugging.md](./dap-debugging.md), [dap-step-backwards.md](./dap-step-backwards.md)

## Overview

Add comprehensive debug logging to the DAP debugging infrastructure that can be toggled from the DAP client. This helps diagnose issues like step conclusions not updating correctly after step-back operations.

## Features

### 1. Debug Log Levels

| Level | Value | What Gets Logged |
|-------|-------|------------------|
| `Off` | 0 | Nothing |
| `Minimal` | 1 | Errors, critical state changes |
| `Normal` | 2 | Step lifecycle, checkpoint operations |
| `Verbose` | 3 | Everything including outputs, expressions |

### 2. Enabling Debug Logging

#### Via Attach Arguments (nvim-dap config)

```lua
{
  type = "runner",
  request = "attach",
  debugLogging = true,           -- Enable debug logging (defaults to "normal" level)
  debugLogLevel = "verbose",     -- Optional: "off", "minimal", "normal", "verbose"
}
```

#### Via REPL Commands (runtime toggle)

| Command | Description |
|---------|-------------|
| `!debug on` | Enable debug logging (level: normal) |
| `!debug off` | Disable debug logging |
| `!debug minimal` | Set level to minimal |
| `!debug normal` | Set level to normal |
| `!debug verbose` | Set level to verbose |
| `!debug status` | Show current debug settings |

### 3. Log Output Format

All debug logs are sent to the DAP console with the format:

```
[DEBUG] [Category] Message
```

Categories include:
- `[Step]` - Step lifecycle events
- `[Checkpoint]` - Checkpoint creation/restoration
- `[StepsContext]` - Steps context mutations (SetOutcome, SetConclusion, SetOutput, ClearScope)

### 4. Example Output

With `!debug verbose` enabled:

```
[DEBUG] [Step] Starting: 'cat doesnotexist' (index=2)
[DEBUG] [Step] Checkpoints available: 2
[DEBUG] [StepsContext] SetOutcome: step='thecat', outcome=failure
[DEBUG] [StepsContext] SetConclusion: step='thecat', conclusion=failure
[DEBUG] [Step] Completed: 'cat doesnotexist', result=Failed
[DEBUG] [Step] Context state: outcome=failure, conclusion=failure

# After step-back:
[DEBUG] [Checkpoint] Restoring checkpoint [1] for step 'cat doesnotexist'
[DEBUG] [StepsContext] ClearScope: scope='(root)'
[DEBUG] [StepsContext] Restoring: clearing scope '(root)', restoring 2 step(s)
[DEBUG] [StepsContext] Restored: step='thefoo', outcome=success, conclusion=success

# After re-running with file created:
[DEBUG] [Step] Starting: 'cat doesnotexist' (index=2)
[DEBUG] [StepsContext] SetOutcome: step='thecat', outcome=success
[DEBUG] [StepsContext] SetConclusion: step='thecat', conclusion=success
[DEBUG] [Step] Completed: 'cat doesnotexist', result=Succeeded
[DEBUG] [Step] Context state: outcome=success, conclusion=success
```

## Implementation

### Progress Checklist

- [x] **Phase 1:** Add debug logging infrastructure to DapDebugSession
- [x] **Phase 2:** Add REPL `!debug` command handling
- [x] **Phase 3:** Add OnDebugLog callback to StepsContext
- [x] **Phase 4:** Add debug logging calls throughout DapDebugSession
- [x] **Phase 5:** Hook up StepsContext logging to DapDebugSession
- [ ] **Phase 6:** Testing

---

### Phase 1: Debug Logging Infrastructure

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

Add enum and helper method:

```csharp
// Add enum for debug log levels (near top of file with other enums)
public enum DebugLogLevel
{
    Off = 0,
    Minimal = 1,    // Errors, critical state changes
    Normal = 2,     // Step lifecycle, checkpoints
    Verbose = 3     // Everything including outputs, expressions
}

// Add field (with other private fields)
private DebugLogLevel _debugLogLevel = DebugLogLevel.Off;

// Add helper method (in a #region Debug Logging)
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
```

Update `HandleAttach` to parse debug logging arguments:

```csharp
private Response HandleAttach(Request request)
{
    Trace.Info("Attach request handled");

    // Parse debug logging from attach args
    if (request.Arguments is JsonElement args)
    {
        if (args.TryGetProperty("debugLogging", out var debugLogging))
        {
            if (debugLogging.ValueKind == JsonValueKind.True)
            {
                _debugLogLevel = DebugLogLevel.Normal;
                Trace.Info("Debug logging enabled via attach args (level: normal)");
            }
        }
        if (args.TryGetProperty("debugLogLevel", out var level) && level.ValueKind == JsonValueKind.String)
        {
            _debugLogLevel = level.GetString()?.ToLower() switch
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
```

---

### Phase 2: REPL `!debug` Command

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

In `HandleEvaluateAsync`, add handling for `!debug` command before other shell command handling:

```csharp
// Near the start of HandleEvaluateAsync, after getting the expression:

// Check for debug command
if (expression.StartsWith("!debug", StringComparison.OrdinalIgnoreCase))
{
    return HandleDebugCommand(expression);
}

// ... rest of existing HandleEvaluateAsync code
```

Add the handler method:

```csharp
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
            break;
        case "off":
            _debugLogLevel = DebugLogLevel.Off;
            result = "Debug logging disabled";
            break;
        case "minimal":
            _debugLogLevel = DebugLogLevel.Minimal;
            result = "Debug logging set to minimal";
            break;
        case "normal":
            _debugLogLevel = DebugLogLevel.Normal;
            result = "Debug logging set to normal";
            break;
        case "verbose":
            _debugLogLevel = DebugLogLevel.Verbose;
            result = "Debug logging set to verbose";
            break;
        case "status":
        default:
            result = $"Debug logging: {_debugLogLevel}";
            break;
    }

    return CreateSuccessResponse(new EvaluateResponseBody
    {
        Result = result,
        VariablesReference = 0
    });
}
```

---

### Phase 3: StepsContext OnDebugLog Callback

**File:** `src/Runner.Worker/StepsContext.cs`

Add callback property and helper:

```csharp
public sealed class StepsContext
{
    private static readonly Regex _propertyRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
    private readonly DictionaryContextData _contextData = new();

    /// <summary>
    /// Optional callback for debug logging. When set, will be called with debug messages
    /// for all StepsContext mutations.
    /// </summary>
    public Action<string> OnDebugLog { get; set; }

    private void DebugLog(string message)
    {
        OnDebugLog?.Invoke(message);
    }

    // ... rest of class
}
```

Update `ClearScope`:

```csharp
public void ClearScope(string scopeName)
{
    DebugLog($"[StepsContext] ClearScope: scope='{scopeName ?? "(root)"}'");
    if (_contextData.TryGetValue(scopeName, out _))
    {
        _contextData[scopeName] = new DictionaryContextData();
    }
}
```

Update `SetOutput`:

```csharp
public void SetOutput(
    string scopeName,
    string stepName,
    string outputName,
    string value,
    out string reference)
{
    var step = GetStep(scopeName, stepName);
    var outputs = step["outputs"].AssertDictionary("outputs");
    outputs[outputName] = new StringContextData(value);
    if (_propertyRegex.IsMatch(outputName))
    {
        reference = $"steps.{stepName}.outputs.{outputName}";
    }
    else
    {
        reference = $"steps['{stepName}']['outputs']['{outputName}']";
    }
    DebugLog($"[StepsContext] SetOutput: step='{stepName}', output='{outputName}', value='{TruncateValue(value)}'");
}

private static string TruncateValue(string value, int maxLength = 50)
{
    if (string.IsNullOrEmpty(value)) return "(empty)";
    if (value.Length <= maxLength) return value;
    return value.Substring(0, maxLength) + "...";
}
```

Update `SetConclusion`:

```csharp
public void SetConclusion(
    string scopeName,
    string stepName,
    ActionResult conclusion)
{
    var step = GetStep(scopeName, stepName);
    var conclusionStr = conclusion.ToString().ToLowerInvariant();
    step["conclusion"] = new StringContextData(conclusionStr);
    DebugLog($"[StepsContext] SetConclusion: step='{stepName}', conclusion={conclusionStr}");
}
```

Update `SetOutcome`:

```csharp
public void SetOutcome(
    string scopeName,
    string stepName,
    ActionResult outcome)
{
    var step = GetStep(scopeName, stepName);
    var outcomeStr = outcome.ToString().ToLowerInvariant();
    step["outcome"] = new StringContextData(outcomeStr);
    DebugLog($"[StepsContext] SetOutcome: step='{stepName}', outcome={outcomeStr}");
}
```

---

### Phase 4: DapDebugSession Logging Calls

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

#### In `OnStepStartingAsync` (after setting `_currentStep` and `_jobContext`):

```csharp
DebugLog($"[Step] Starting: '{step.DisplayName}' (index={stepIndex})");
DebugLog($"[Step] Checkpoints available: {_checkpoints.Count}");
```

#### In `OnStepCompleted` (after logging to Trace):

```csharp
DebugLog($"[Step] Completed: '{step.DisplayName}', result={result}");

// Log current steps context state for this step
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
```

#### In `CreateCheckpointForPendingStep` (after creating checkpoint):

```csharp
DebugLog($"[Checkpoint] Created [{_checkpoints.Count - 1}] for step '{_pendingStep.DisplayName}'");
if (_debugLogLevel >= DebugLogLevel.Verbose)
{
    DebugLog($"[Checkpoint] Snapshot contains {checkpoint.StepsSnapshot.Count} step(s)", DebugLogLevel.Verbose);
    foreach (var entry in checkpoint.StepsSnapshot)
    {
        DebugLog($"[Checkpoint]   {entry.Key}: outcome={entry.Value.Outcome}, conclusion={entry.Value.Conclusion}", DebugLogLevel.Verbose);
    }
}
```

#### In `RestoreCheckpoint` (at start of method):

```csharp
DebugLog($"[Checkpoint] Restoring [{checkpointIndex}] for step '{checkpoint.StepDisplayName}'");
if (_debugLogLevel >= DebugLogLevel.Verbose)
{
    DebugLog($"[Checkpoint] Snapshot has {checkpoint.StepsSnapshot.Count} step(s)", DebugLogLevel.Verbose);
}
```

#### In `RestoreStepsContext` (update existing method):

```csharp
private void RestoreStepsContext(StepsContext stepsContext, Dictionary<string, StepStateSnapshot> snapshot, string scopeName)
{
    scopeName = scopeName ?? string.Empty;

    DebugLog($"[StepsContext] Restoring: clearing scope '{(string.IsNullOrEmpty(scopeName) ? "(root)" : scopeName)}', will restore {snapshot.Count} step(s)");

    stepsContext.ClearScope(scopeName);

    foreach (var entry in snapshot)
    {
        var key = entry.Key;
        var slashIndex = key.IndexOf('/');

        if (slashIndex >= 0)
        {
            var snapshotScopeName = slashIndex > 0 ? key.Substring(0, slashIndex) : string.Empty;
            var stepName = key.Substring(slashIndex + 1);

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
```

---

### Phase 5: Hook Up StepsContext Logging

**File:** `src/Runner.Worker/Dap/DapDebugSession.cs`

In `OnStepStartingAsync`, after setting `_jobContext`, hook up the callback (only once):

```csharp
// Hook up StepsContext debug logging (do this once when we first get jobContext)
if (jobContext.Global.StepsContext.OnDebugLog == null)
{
    jobContext.Global.StepsContext.OnDebugLog = (msg) => DebugLog(msg, DebugLogLevel.Verbose);
}
```

**Note:** StepsContext logging is set to `Verbose` level since `SetOutput` can be noisy. `SetConclusion` and `SetOutcome` will still appear at `Verbose` level, but all the important state changes are also logged directly in `OnStepCompleted` at `Normal` level.

---

### Phase 6: Testing

#### Manual Testing Checklist

- [ ] `!debug status` shows "Off" by default
- [ ] `!debug on` enables logging, shows step lifecycle
- [ ] `!debug verbose` shows StepsContext mutations
- [ ] `!debug off` disables logging
- [ ] Attach with `debugLogging: true` enables logging on connect
- [ ] Attach with `debugLogLevel: "verbose"` sets correct level
- [ ] Step-back scenario shows restoration logs
- [ ] Logs help identify why conclusion might not update

#### Test Workflow

Use the test workflow with `thecat` step:
1. Run workflow, let `thecat` fail
2. Enable `!debug verbose`
3. Step back
4. Create the missing file
5. Step forward
6. Observe logs to see if `SetConclusion` is called with `success`

---

## Files Summary

### Modified Files

| File | Changes |
|------|---------|
| `src/Runner.Worker/Dap/DapDebugSession.cs` | Add `DebugLogLevel` enum, `_debugLogLevel` field, `DebugLog()` helper, `HandleDebugCommand()`, update `HandleAttach`, add logging calls throughout, hook up StepsContext callback |
| `src/Runner.Worker/StepsContext.cs` | Add `OnDebugLog` callback, `DebugLog()` helper, `TruncateValue()` helper, add logging to `ClearScope`, `SetOutput`, `SetConclusion`, `SetOutcome` |

---

## Future Enhancements (Out of Scope)

- Additional debug commands (`!debug checkpoints`, `!debug steps`, `!debug env`)
- Log to file option
- Structured logging with timestamps
- Category-based filtering (e.g., only show `[StepsContext]` logs)
- Integration with nvim-dap's virtual text for inline debug info
