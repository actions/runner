using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Handlers;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Executes <see cref="RunCommand"/> objects in the job's runtime context.
    ///
    /// Mirrors the behavior of a normal workflow <c>run:</c> step as closely
    /// as possible by reusing the runner's existing shell-resolution logic,
    /// script fixup helpers, and process execution infrastructure.
    ///
    /// Output is streamed to the debugger via DAP <c>output</c> events with
    /// secrets masked before emission.
    /// </summary>
    internal sealed class DapReplExecutor
    {
        private readonly IHostContext _hostContext;
        private readonly Action<string, string> _sendOutput;
        private readonly Tracing _trace;

        public DapReplExecutor(IHostContext hostContext, Action<string, string> sendOutput)
        {
            _hostContext = hostContext ?? throw new ArgumentNullException(nameof(hostContext));
            _sendOutput = sendOutput ?? throw new ArgumentNullException(nameof(sendOutput));
            _trace = hostContext.GetTrace(nameof(DapReplExecutor));
        }

        /// <summary>
        /// Executes a <see cref="RunCommand"/> and returns the exit code as a
        /// formatted <see cref="EvaluateResponseBody"/>.
        /// </summary>
        public async Task<EvaluateResponseBody> ExecuteRunCommandAsync(
            RunCommand command,
            IExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                return ErrorResult("No execution context available. The debugger must be paused at a step to run commands.");
            }

            try
            {
                return await ExecuteScriptAsync(command, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _trace.Error($"REPL run command failed ({ex.GetType().Name})");
                var maskedError = _hostContext.SecretMasker.MaskSecrets(ex.Message);
                return ErrorResult($"Command failed: {maskedError}");
            }
        }

        private async Task<EvaluateResponseBody> ExecuteScriptAsync(
            RunCommand command,
            IExecutionContext context,
            CancellationToken cancellationToken)
        {
            // 1. Resolve shell — same logic as ScriptHandler
            string shellCommand;
            string argFormat;

            if (!string.IsNullOrEmpty(command.Shell))
            {
                // Explicit shell from the DSL
                var parsed = ScriptHandlerHelpers.ParseShellOptionString(command.Shell);
                shellCommand = parsed.shellCommand;
                argFormat = string.IsNullOrEmpty(parsed.shellArgs)
                    ? ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand)
                    : parsed.shellArgs;
            }
            else
            {
                // Default shell — mirrors ScriptHandler platform defaults
                shellCommand = ResolveDefaultShell(context);
                argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
            }

            _trace.Info("Resolved REPL shell");

            // 2. Expand ${{ }} expressions in the script body, just like
            //    ActionRunner evaluates step inputs before ScriptHandler sees them
            var contents = ExpandExpressions(command.Script, context);
            contents = ScriptHandlerHelpers.FixUpScriptContents(shellCommand, contents);

            // Write to a temp file (same pattern as ScriptHandler)
            var extension = ScriptHandlerHelpers.GetScriptFileExtension(shellCommand);
            var scriptFilePath = Path.Combine(
                _hostContext.GetDirectory(WellKnownDirectory.Temp),
                $"dap_repl_{Guid.NewGuid()}{extension}");

            Encoding encoding = new UTF8Encoding(false);
#if OS_WINDOWS
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            encoding = Console.InputEncoding.CodePage != 65001
                ? Console.InputEncoding
                : encoding;
#endif
            File.WriteAllText(scriptFilePath, contents, encoding);

            try
            {
                // 3. Format arguments with script path
                var resolvedPath = scriptFilePath.Replace("\"", "\\\"");
                if (string.IsNullOrEmpty(argFormat) || !argFormat.Contains("{0}"))
                {
                    return ErrorResult($"Invalid shell option '{shellCommand}'. Shell must be a valid built-in (bash, sh, cmd, powershell, pwsh) or a format string containing '{{0}}'");
                }
                var arguments = string.Format(argFormat, resolvedPath);

                // 4. Resolve shell command path
                string prependPath = string.Join(
                    Path.PathSeparator.ToString(),
                    Enumerable.Reverse(context.Global.PrependPath));
                var commandPath = WhichUtil.Which(shellCommand, false, _trace, prependPath)
                    ?? shellCommand;

                // 5. Build environment — merge from execution context like a real step
                var environment = BuildEnvironment(context, command.Env);

                // 6. Resolve working directory
                var workingDirectory = command.WorkingDirectory;
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    var githubContext = context.ExpressionValues.TryGetValue("github", out var gh)
                        ? gh as DictionaryContextData
                        : null;
                    var workspace = githubContext?.TryGetValue("workspace", out var ws) == true
                        ? (ws as StringContextData)?.Value
                        : null;
                    workingDirectory = workspace ?? _hostContext.GetDirectory(WellKnownDirectory.Work);
                }

                _trace.Info("Executing REPL command");

                // Stream execution info to debugger
                SendOutput("console", $"$ {shellCommand} {command.Script.Substring(0, Math.Min(command.Script.Length, 80))}{(command.Script.Length > 80 ? "..." : "")}\n");

                // 7. Execute via IProcessInvoker (same as DefaultStepHost)
                int exitCode;
                using (var processInvoker = _hostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            var masked = _hostContext.SecretMasker.MaskSecrets(args.Data);
                            SendOutput("stdout", masked + "\n");
                        }
                    };

                    processInvoker.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            var masked = _hostContext.SecretMasker.MaskSecrets(args.Data);
                            SendOutput("stderr", masked + "\n");
                        }
                    };

                    exitCode = await processInvoker.ExecuteAsync(
                        workingDirectory: workingDirectory,
                        fileName: commandPath,
                        arguments: arguments,
                        environment: environment,
                        requireExitCodeZero: false,
                        outputEncoding: null,
                        killProcessOnCancel: true,
                        cancellationToken: cancellationToken);
                }

                _trace.Info($"REPL command exited with code {exitCode}");

                // 8. Return only the exit code summary (output was already streamed)
                return new EvaluateResponseBody
                {
                    Result = exitCode == 0 ? $"(exit code: {exitCode})" : $"Process completed with exit code {exitCode}.",
                    Type = exitCode == 0 ? "string" : "error",
                    VariablesReference = 0
                };
            }
            finally
            {
                // Clean up temp script file
                try { File.Delete(scriptFilePath); }
                catch { /* best effort */ }
            }
        }

        /// <summary>
        /// Expands <c>${{ }}</c> expressions in the input string using the
        /// runner's template evaluator — the same evaluation path that processes
        /// step inputs before <see cref="ScriptHandler"/> runs them.
        ///
        /// Each <c>${{ expr }}</c> occurrence is individually evaluated and
        /// replaced with its masked string result, mirroring the semantics of
        /// expression interpolation in a workflow <c>run:</c> step body.
        /// </summary>
        internal string ExpandExpressions(string input, IExecutionContext context)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains("${{"))
            {
                return input ?? string.Empty;
            }

            var result = new StringBuilder();
            int pos = 0;

            while (pos < input.Length)
            {
                var start = input.IndexOf("${{", pos, StringComparison.Ordinal);
                if (start < 0)
                {
                    result.Append(input, pos, input.Length - pos);
                    break;
                }

                // Append the literal text before the expression
                result.Append(input, pos, start - pos);

                var end = input.IndexOf("}}", start + 3, StringComparison.Ordinal);
                if (end < 0)
                {
                    // Unterminated expression — keep literal
                    result.Append(input, start, input.Length - start);
                    break;
                }

                var expr = input.Substring(start + 3, end - start - 3).Trim();
                end += 2; // skip past "}}"

                // Evaluate the expression
                try
                {
                    var templateEvaluator = context.ToPipelineTemplateEvaluator();
                    var token = new GitHub.DistributedTask.ObjectTemplating.Tokens.BasicExpressionToken(
                        null, null, null, expr);
                    var evaluated = templateEvaluator.EvaluateStepDisplayName(
                        token,
                        context.ExpressionValues,
                        context.ExpressionFunctions);
                    result.Append(_hostContext.SecretMasker.MaskSecrets(evaluated ?? string.Empty));
                }
                catch (Exception ex)
                {
                    _trace.Warning($"Expression expansion failed ({ex.GetType().Name})");
                    // Keep the original expression literal on failure
                    result.Append(input, start, end - start);
                }

                pos = end;
            }

            return result.ToString();
        }

        /// <summary>
        /// Resolves the default shell the same way <see cref="ScriptHandler"/>
        /// does: check job defaults, then fall back to platform default.
        /// </summary>
        internal string ResolveDefaultShell(IExecutionContext context)
        {
            // Check job defaults
            if (context.Global?.JobDefaults != null &&
                context.Global.JobDefaults.TryGetValue("run", out var runDefaults) &&
                runDefaults.TryGetValue("shell", out var defaultShell) &&
                !string.IsNullOrEmpty(defaultShell))
            {
                _trace.Info("Using job default shell");
                return defaultShell;
            }

#if OS_WINDOWS
            string prependPath = string.Join(
                Path.PathSeparator.ToString(),
                context.Global?.PrependPath != null ? Enumerable.Reverse(context.Global.PrependPath) : Array.Empty<string>());
            var pwshPath = WhichUtil.Which("pwsh", false, _trace, prependPath);
            return !string.IsNullOrEmpty(pwshPath) ? "pwsh" : "powershell";
#else
            return "sh";
#endif
        }

        /// <summary>
        /// Merges the job context environment with any REPL-specific overrides.
        /// </summary>
        internal Dictionary<string, string> BuildEnvironment(
            IExecutionContext context,
            Dictionary<string, string> replEnv)
        {
            var env = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer);

            // Pull environment from the execution context (same as ActionRunner)
            if (context.ExpressionValues.TryGetValue("env", out var envData))
            {
                if (envData is DictionaryContextData dictEnv)
                {
                    foreach (var pair in dictEnv)
                    {
                        if (pair.Value is StringContextData str)
                        {
                            env[pair.Key] = str.Value;
                        }
                    }
                }
                else if (envData is CaseSensitiveDictionaryContextData csEnv)
                {
                    foreach (var pair in csEnv)
                    {
                        if (pair.Value is StringContextData str)
                        {
                            env[pair.Key] = str.Value;
                        }
                    }
                }
            }

            // Expose runtime context variables to the environment (GITHUB_*, RUNNER_*, etc.)
            foreach (var ctxPair in context.ExpressionValues)
            {
                if (ctxPair.Value is IEnvironmentContextData runtimeContext && runtimeContext != null)
                {
                    foreach (var rtEnv in runtimeContext.GetRuntimeEnvironmentVariables())
                    {
                        env[rtEnv.Key] = rtEnv.Value;
                    }
                }
            }

            // Apply REPL-specific overrides last (so they win),
            // expanding any ${{ }} expressions in the values
            if (replEnv != null)
            {
                foreach (var pair in replEnv)
                {
                    env[pair.Key] = ExpandExpressions(pair.Value, context);
                }
            }

            return env;
        }

        private void SendOutput(string category, string text)
        {
            _sendOutput(category, text);
        }

        private static EvaluateResponseBody ErrorResult(string message)
        {
            return new EvaluateResponseBody
            {
                Result = message,
                Type = "error",
                VariablesReference = 0
            };
        }
    }
}
