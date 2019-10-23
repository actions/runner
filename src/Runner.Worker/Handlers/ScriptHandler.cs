﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ScriptHandler))]
    public interface IScriptHandler : IHandler
    {
        ScriptActionExecutionData Data { get; set; }
    }

    public sealed class ScriptHandler : Handler, IScriptHandler
    {
        public ScriptActionExecutionData Data { get; set; }

        public override void PrintActionDetails(ActionRunStage stage)
        {
            if (stage == ActionRunStage.Post)
            {
                throw new NotSupportedException("Script action should not have 'Post' job action.");
            }

            Inputs.TryGetValue("script", out string contents);
            contents = contents ?? string.Empty;
            if (Action.Type == Pipelines.ActionSourceType.Script)
            {
                var firstLine = contents.TrimStart(' ', '\t', '\r', '\n');
                var firstNewLine = firstLine.IndexOfAny(new[] { '\r', '\n' });
                if (firstNewLine >= 0)
                {
                    firstLine = firstLine.Substring(0, firstNewLine);
                }

                ExecutionContext.Output($"##[group]Run {firstLine}");
            }
            else
            {
                throw new InvalidOperationException($"Invalid action type {Action.Type} for {nameof(ScriptHandler)}");
            }

            var multiLines = contents.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            foreach (var line in multiLines)
            {
                // Bright Cyan color
                ExecutionContext.Output($"\x1b[36;1m{line}\x1b[0m");
            }

            string argFormat;
            string shellCommand;
            string shellCommandPath = null;
            bool validateShellOnHost = !(StepHost is ContainerStepHost);
            Inputs.TryGetValue("shell", out var shell);
            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS
                shellCommand = "pwsh";
                if(validateShellOnHost)
                {
                    shellCommandPath = WhichUtil.Which(shellCommand, require: false, Trace);
                    if (string.IsNullOrEmpty(shellCommandPath))
                    {
                        shellCommand = "powershell";
                        Trace.Info($"Defaulting to {shellCommand}");
                        shellCommandPath = WhichUtil.Which(shellCommand, require: true, Trace);
                    }
                }
#else
                shellCommand = "sh";
                if (validateShellOnHost)
                {
                    shellCommandPath = WhichUtil.Which("bash") ?? WhichUtil.Which("sh", true, Trace);
                }
#endif
                argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
            }
            else
            {
                var parsed = ScriptHandlerHelpers.ParseShellOptionString(shell);
                shellCommand = parsed.shellCommand;
                if (validateShellOnHost)
                {
                    shellCommandPath = WhichUtil.Which(parsed.shellCommand, true, Trace);
                }

                argFormat = $"{parsed.shellArgs}".TrimStart();
                if (string.IsNullOrEmpty(argFormat))
                {
                    argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
                }
            }

            if (!string.IsNullOrEmpty(shellCommandPath))
            {
                ExecutionContext.Output($"shell: {shellCommandPath} {argFormat}");
            }
            else
            {
                ExecutionContext.Output($"shell: {shellCommand} {argFormat}");
            }

            if (this.Environment?.Count > 0)
            {
                ExecutionContext.Output("env:");
                foreach (var env in this.Environment)
                {
                    ExecutionContext.Output($"  {env.Key}: {env.Value}");
                }
            }

            ExecutionContext.Output("##[endgroup]");
        }

        public async Task RunAsync(ActionRunStage stage)
        {
            if (stage == ActionRunStage.Post)
            {
                throw new NotSupportedException("Script action should not have 'Post' job action.");
            }

            // Validate args
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            Inputs.TryGetValue("script", out var contents);
            contents = contents ?? string.Empty;

            Inputs.TryGetValue("workingDirectory", out var workingDirectory);
            var workspaceDir = githubContext["workspace"] as StringContextData;
            workingDirectory = Path.Combine(workspaceDir, workingDirectory ?? string.Empty);

            Inputs.TryGetValue("shell", out var shell);
            var isContainerStepHost = StepHost is ContainerStepHost;

            string commandPath, argFormat, shellCommand;
            // Set up default command and arguments
            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS
                shellCommand = "pwsh";
                commandPath = WhichUtil.Which(shellCommand, require: false, Trace);
                if (string.IsNullOrEmpty(commandPath))
                {
                    shellCommand = "powershell";
                    Trace.Info($"Defaulting to {shellCommand}");
                    commandPath = WhichUtil.Which(shellCommand, require: true, Trace);
                }
                ArgUtil.NotNullOrEmpty(commandPath, "Default Shell");
#else
                shellCommand = "sh";
                commandPath = WhichUtil.Which("bash", false, Trace) ?? WhichUtil.Which("sh", true, Trace);
#endif
                argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
            }
            else
            {
                var parsed = ScriptHandlerHelpers.ParseShellOptionString(shell);
                shellCommand = parsed.shellCommand;
                // For non-ContainerStepHost, the command must be located on the host by Which
                commandPath = WhichUtil.Which(parsed.shellCommand, !isContainerStepHost, Trace);
                argFormat = $"{parsed.shellArgs}".TrimStart();
                if (string.IsNullOrEmpty(argFormat))
                {
                    argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
                }
            }

            // No arg format was given, shell must be a built-in
            if (string.IsNullOrEmpty(argFormat) || !argFormat.Contains("{0}"))
            {
                throw new ArgumentException("Invalid shell option. Shell must be a valid built-in (bash, sh, cmd, powershell, pwsh) or a format string containing '{0}'");
            }

            // We do not not the full path until we know what shell is being used, so that we can determine the file extension
            var scriptFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}{ScriptHandlerHelpers.GetScriptFileExtension(shellCommand)}");
            var resolvedScriptPath = $"{StepHost.ResolvePathForStepHost(scriptFilePath).Replace("\"", "\\\"")}";

            // Format arg string with script path
            var arguments = string.Format(argFormat, resolvedScriptPath);

            // Fix up and write the script
            contents = ScriptHandlerHelpers.FixUpScriptContents(shellCommand, contents);
#if OS_WINDOWS
            // Normalize Windows line endings
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            var encoding = ExecutionContext.Variables.Retain_Default_Encoding && Console.InputEncoding.CodePage != 65001
                ? Console.InputEncoding
                : new UTF8Encoding(false);
#else
            // Don't add a BOM. It causes the script to fail on some operating systems (e.g. on Ubuntu 14).
            var encoding = new UTF8Encoding(false);
#endif
            // Script is written to local path (ie host) but executed relative to the StepHost, which may be a container
            File.WriteAllText(scriptFilePath, contents, encoding);

            // Prepend PATH
            AddPrependPathToEnvironment();

            // expose context to environment
            foreach (var context in ExecutionContext.ExpressionValues)
            {
                if (context.Value is IEnvironmentContextData runtimeContext && runtimeContext != null)
                {
                    foreach (var env in runtimeContext.GetRuntimeEnvironmentVariables())
                    {
                        Environment[env.Key] = env.Value;
                    }
                }
            }

            // dump out the command
            var fileName = isContainerStepHost ? shellCommand : commandPath;
            ExecutionContext.Debug($"{fileName} {arguments}");

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                StepHost.OutputDataReceived += stdoutManager.OnDataReceived;
                StepHost.ErrorDataReceived += stderrManager.OnDataReceived;

                // Execute
                int exitCode = await StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                            fileName: fileName,
                                            arguments: arguments,
                                            environment: Environment,
                                            requireExitCodeZero: false,
                                            outputEncoding: null,
                                            killProcessOnCancel: false,
                                            inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                            cancellationToken: ExecutionContext.CancellationToken);

                // Error
                if (exitCode != 0)
                {
                    ExecutionContext.Error($"Process completed with exit code {exitCode}.");
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }
        }
    }
}
