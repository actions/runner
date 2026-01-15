# DAP Debugging - Bug Fixes and Enhancements

**Status:** Planned  
**Date:** January 2026  
**Related:** [dap-debugging.md](./dap-debugging.md)

## Overview

This document tracks bug fixes and enhancements for the DAP debugging implementation after the initial phases were completed.

## Issues

### Bug 1: Double Output in REPL Shell Commands

**Symptom:** Running commands in the REPL shell produces double output - the first one unmasked, the second one with secrets masked.

**Root Cause:** In `DapDebugSession.ExecuteShellCommandAsync()` (lines 670-773), output is sent to the debugger twice:

1. **Real-time streaming (unmasked):** Lines 678-712 stream output via DAP `output` events as data arrives from the process - but this output is NOT masked
2. **Final result (masked):** Lines 765-769 return the combined output as `EvaluateResponseBody.Result` with secrets masked

The DAP client displays both the streamed events AND the evaluate response result, causing duplication.

**Fix:**
1. Mask secrets in the real-time streaming output (add `HostContext.SecretMasker.MaskSecrets()` to lines ~690 and ~708)
2. Change the final `Result` to only show exit code summary instead of full output

---

### Bug 2: Expressions Interpreted as Shell Commands

**Symptom:** Evaluating expressions like `${{github.event_name}} == 'push'` in the Watch/Expressions pane results in them being executed as shell commands instead of being evaluated as GitHub Actions expressions.

**Root Cause:** In `DapDebugSession.HandleEvaluateAsync()` (line 514), the condition to detect shell commands is too broad:

```csharp
if (evalContext == "repl" || expression.StartsWith("!") || expression.StartsWith("$"))
```

Since `${{github.event_name}}` starts with `$`, it gets routed to shell execution instead of expression evaluation.

**Fix:**
1. Check for `${{` prefix first - these are always GitHub Actions expressions
2. Remove the `expression.StartsWith("$")` condition entirely (ambiguous and unnecessary since REPL context handles shell commands)
3. Keep `expression.StartsWith("!")` for explicit shell override in non-REPL contexts

---

### Enhancement: Expression Interpolation in REPL Commands

**Request:** When running REPL commands like `echo ${{github.event_name}}`, the `${{ }}` expressions should be expanded before shell execution, similar to how `run:` steps work.

**Approach:** Add a helper method that uses the existing `PipelineTemplateEvaluator` infrastructure to expand expressions in the command string before passing it to the shell.

---

## Implementation Details

### File: `src/Runner.Worker/Dap/DapDebugSession.cs`

#### Change 1: Mask Real-Time Streaming Output

**Location:** Lines ~678-712 (OutputDataReceived and ErrorDataReceived handlers)

**Before:**
```csharp
processInvoker.OutputDataReceived += (sender, args) =>
{
    if (!string.IsNullOrEmpty(args.Data))
    {
        output.AppendLine(args.Data);
        _server?.SendEvent(new Event
        {
            EventType = "output",
            Body = new OutputEventBody
            {
                Category = "stdout",
                Output = args.Data + "\n"  // NOT MASKED
            }
        });
    }
};
```

**After:**
```csharp
processInvoker.OutputDataReceived += (sender, args) =>
{
    if (!string.IsNullOrEmpty(args.Data))
    {
        output.AppendLine(args.Data);
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
```

Apply the same change to `ErrorDataReceived` handler (~lines 696-712).

---

#### Change 2: Return Only Exit Code in Result

**Location:** Lines ~767-772 (return statement in ExecuteShellCommandAsync)

**Before:**
```csharp
return new EvaluateResponseBody
{
    Result = result.TrimEnd('\r', '\n'),
    Type = exitCode == 0 ? "string" : "error",
    VariablesReference = 0
};
```

**After:**
```csharp
return new EvaluateResponseBody
{
    Result = $"(exit code: {exitCode})",
    Type = exitCode == 0 ? "string" : "error",
    VariablesReference = 0
};
```

Also remove the result combination logic (lines ~747-762) since we no longer need to build the full result string for the response.

---

#### Change 3: Fix Expression vs Shell Routing

**Location:** Lines ~511-536 (HandleEvaluateAsync method)

**Before:**
```csharp
try
{
    // Check if this is a REPL/shell command (context: "repl") or starts with shell prefix
    if (evalContext == "repl" || expression.StartsWith("!") || expression.StartsWith("$"))
    {
        // Shell execution mode
        var command = expression.TrimStart('!', '$').Trim();
        // ...
    }
    else
    {
        // Expression evaluation mode
        var result = EvaluateExpression(expression, executionContext);
        return CreateSuccessResponse(result);
    }
}
```

**After:**
```csharp
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
```

---

#### Change 4: Add Expression Expansion Helper Method

**Location:** Add new method before `ExecuteShellCommandAsync` (~line 667)

```csharp
/// <summary>
/// Expands ${{ }} expressions within a command string.
/// For example: "echo ${{github.event_name}}" -> "echo push"
/// </summary>
private string ExpandExpressionsInCommand(string command, IExecutionContext context)
{
    if (string.IsNullOrEmpty(command) || !command.Contains("${{"))
    {
        return command;
    }

    try
    {
        // Create a StringToken with the command
        var token = new StringToken(null, null, null, command);

        // Use the template evaluator to expand expressions
        var templateEvaluator = context.ToPipelineTemplateEvaluator();
        var result = templateEvaluator.EvaluateStepDisplayName(
            token,
            context.ExpressionValues,
            context.ExpressionFunctions);

        // Mask secrets in the expanded command
        result = HostContext.SecretMasker.MaskSecrets(result ?? command);

        Trace.Info($"Expanded command: {result}");
        return result;
    }
    catch (Exception ex)
    {
        Trace.Info($"Expression expansion failed, using original command: {ex.Message}");
        return command;
    }
}
```

**Required import:** Add `using GitHub.DistributedTask.ObjectTemplating.Tokens;` at the top of the file if not already present.

---

#### Change 5: Use Expression Expansion in Shell Execution

**Location:** Beginning of `ExecuteShellCommandAsync` method (~line 670)

**Before:**
```csharp
private async Task<EvaluateResponseBody> ExecuteShellCommandAsync(string command, IExecutionContext context)
{
    Trace.Info($"Executing shell command: {command}");
    // ...
}
```

**After:**
```csharp
private async Task<EvaluateResponseBody> ExecuteShellCommandAsync(string command, IExecutionContext context)
{
    // Expand ${{ }} expressions in the command first
    command = ExpandExpressionsInCommand(command, context);

    Trace.Info($"Executing shell command: {command}");
    // ...
}
```

---

## DAP Context Reference

For future reference, these are the DAP evaluate context values:

| DAP Context | Source UI | Behavior |
|-------------|-----------|----------|
| `"repl"` | Debug Console / REPL pane | Shell execution (with expression expansion) |
| `"watch"` | Watch / Expressions pane | Expression evaluation |
| `"hover"` | Editor hover (default) | Expression evaluation |
| `"variables"` | Variables pane | Expression evaluation |
| `"clipboard"` | Copy to clipboard | Expression evaluation |

---

## Testing Checklist

- [ ] REPL command output is masked and appears only once
- [ ] REPL command shows exit code in result field
- [ ] Expression `${{github.event_name}}` evaluates correctly in Watch pane
- [ ] Expression `${{github.event_name}} == 'push'` evaluates correctly
- [ ] REPL command `echo ${{github.event_name}}` expands and executes correctly
- [ ] REPL command `!ls -la` from Watch pane works (explicit shell prefix)
- [ ] Secrets are masked in all outputs (streaming and expanded commands)
