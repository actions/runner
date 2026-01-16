# DAP Step Backward: Duplicate Expression Function Fix

**Status:** Ready for Implementation  
**Date:** January 2026  
**Related:** [dap-step-backwards.md](./dap-step-backwards.md)

## Problem

When stepping backward and then forward again during DAP debugging, the runner crashes with:

```
System.ArgumentException: An item with the same key has already been added. Key: always
   at System.Collections.Generic.Dictionary`2.TryInsert(...)
   at GitHub.DistributedTask.Expressions2.ExpressionParser.ParseContext..ctor(...)
```

### Reproduction Steps

1. Run a workflow with DAP debugging enabled
2. Let a step execute (e.g., `cat doesnotexist`)
3. Before the next step runs, step backward
4. Optionally run REPL commands
5. Step forward to re-run the step
6. Step forward again → **CRASH**

## Root Cause Analysis

### The Bug

In `StepsRunner.cs:89-93`, expression functions are added to `step.ExecutionContext.ExpressionFunctions` every time a step is processed:

```csharp
// Expression functions
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));
```

### Why It Fails on Step-Back

1. **First execution:** Step is dequeued, functions added to `ExpressionFunctions`, step runs
2. **Checkpoint created:** Stores a **reference** to the `IStep` object (not a deep copy) - see `StepCheckpoint.cs:65`
3. **Step backward:** Checkpoint is restored, the **same** `IStep` object is re-queued to `jobContext.JobSteps`
4. **Second execution:** Step is dequeued again, functions added **again** to the same `ExpressionFunctions` list
5. **Duplicate entries:** The list now has two `AlwaysFunction` entries, two `CancelledFunction` entries, etc.
6. **Crash:** When `ExpressionParser.ParseContext` constructor iterates over functions and adds them to a `Dictionary` (`ExpressionParser.cs:460-465`), it throws on the duplicate key "always"

### Key Insight

The `ExpressionFunctions` property on `ExecutionContext` is a `List<IFunctionInfo>` (`ExecutionContext.cs:199`). `List<T>.Add()` doesn't check for duplicates, so the functions get added twice. The error only manifests later when the expression parser builds its internal dictionary.

## Solution

### Chosen Approach: Clear ExpressionFunctions Before Adding

Clear the `ExpressionFunctions` list before adding the functions. This ensures a known state regardless of how the step arrived in the queue (fresh or restored from checkpoint).

### Why This Approach

| Approach | Pros | Cons |
|----------|------|------|
| **Clear before adding (chosen)** | Simple, explicit, ensures known state, works for any re-processing scenario | Slightly more work than strictly necessary on first run |
| Check before adding | Defensive | More complex, multiple conditions to check |
| Reset on checkpoint restore | Localized to DAP | Requires changes in multiple places, easy to miss edge cases |

The "clear before adding" approach is:
- **Simple:** One line of code
- **Robust:** Works regardless of why the step is being re-processed
- **Safe:** The functions are always the same set, so clearing and re-adding has no side effects
- **Future-proof:** If other code paths ever re-queue steps, this handles it automatically

## Implementation

### File to Modify

`src/Runner.Worker/StepsRunner.cs`

### Change

```csharp
// Before line 88, add:
step.ExecutionContext.ExpressionFunctions.Clear();

// Expression functions
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
// ... rest of the adds
```

### Full Context (lines ~85-94)

**Before:**
```csharp
// Start
step.ExecutionContext.Start();

// Expression functions
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));
```

**After:**
```csharp
// Start
step.ExecutionContext.Start();

// Expression functions
// Clear first to handle step-back scenarios where the same step may be re-processed
step.ExecutionContext.ExpressionFunctions.Clear();
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));
```

## Testing

### Manual Test Scenario

1. Create a workflow with multiple steps
2. Enable DAP debugging
3. Let step 1 execute
4. Pause before step 2
5. Step backward (restore to before step 1)
6. Step forward (re-run step 1)
7. Step forward again (run step 2)
8. **Verify:** No crash, step 2's condition evaluates correctly

### Edge Cases to Verify

- [ ] Step backward multiple times in a row
- [ ] Step backward then run REPL commands, then step forward
- [ ] `reverseContinue` to beginning, then step through all steps again
- [ ] Steps with `if: always()` condition (the specific function that was failing)
- [ ] Steps with `if: failure()` or `if: cancelled()` conditions

## Risk Assessment

**Risk: Low**

- The fix is minimal (one line)
- `ExpressionFunctions` is always populated with the same 5 functions at this point
- No other code depends on functions being accumulated across step re-runs
- Normal (non-DAP) execution is unaffected since steps are never re-queued

## Files Summary

| File | Change |
|------|--------|
| `src/Runner.Worker/StepsRunner.cs` | Add `Clear()` call before adding expression functions |
