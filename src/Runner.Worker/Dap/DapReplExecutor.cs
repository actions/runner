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
        private readonly IDapServer _server;
        private readonly Tracing _trace;

        public DapReplExecutor(IHostContext hostContext, IDapServer server)
        {
            _hostContext = hostContext ?? throw new ArgumentNullException(nameof(hostContext));
            _server = server;
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
                _trace.Error($"REPL run command failed: {ex}");
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

            _trace.Info($"REPL shell: {shellCommand}, argFormat: {argFormat}");

            // 2. Prepare the script content
            var contents = command.Script;
            contents = ScriptHandlerHelpers.FixUpScriptContents(shellCommand, contents);

            // Write to a temp file (same pattern as ScriptHandler)
            var extension = ScriptHandlerHelpers.GetScriptFileExtension(shellCommand);
            var scriptFilePath = Path.Combine(
                _hostContext.GetDirectory(WellKnownDirectory.Temp),
                $"dap_repl_{Guid.NewGuid()}{extension}");

            var encoding = new UTF8Encoding(false);
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

                _trace.Info($"REPL executing: {commandPath} {arguments} (cwd: {workingDirectory})");

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
        /// Resolves the default shell the same way <see cref="ScriptHandler"/>
        /// does: check job defaults, then fall back to platform default.
        /// </summary>
        private string ResolveDefaultShell(IExecutionContext context)
        {
            // Check job defaults
            if (context.Global?.JobDefaults != null &&
                context.Global.JobDefaults.TryGetValue("run", out var runDefaults) &&
                runDefaults.TryGetValue("shell", out var defaultShell) &&
                !string.IsNullOrEmpty(defaultShell))
            {
                _trace.Info($"Using job default shell: {defaultShell}");
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
        private Dictionary<string, string> BuildEnvironment(
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

            // Apply REPL-specific overrides last (so they win)
            if (replEnv != null)
            {
                foreach (var pair in replEnv)
                {
                    env[pair.Key] = pair.Value;
                }
            }

            return env;
        }

        private void SendOutput(string category, string text)
        {
            _server?.SendEvent(new Event
            {
                EventType = "output",
                Body = new OutputEventBody
                {
                    Category = category,
                    Output = text
                }
            });
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
