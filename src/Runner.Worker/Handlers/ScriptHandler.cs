﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
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

        protected override void PrintActionDetails(ActionRunStage stage)
        {
            // if we're executing a Job Extension, we won't have an 'Action'
            if (!IsActionStep)
            {
                if (Inputs.TryGetValue("path", out var path))
                {
                    ExecutionContext.Output($"##[group]Run '{path}'");
                }
                else
                {
                    throw new InvalidOperationException("Inputs 'path' must be set for job extensions");
                }
            }
            else if (Action.Type == Pipelines.ActionSourceType.Script)
            {
                Inputs.TryGetValue("script", out string contents);
                contents = contents ?? string.Empty;
                var firstLine = contents.TrimStart(' ', '\t', '\r', '\n');
                var firstNewLine = firstLine.IndexOfAny(new[] { '\r', '\n' });
                if (firstNewLine >= 0)
                {
                    firstLine = firstLine.Substring(0, firstNewLine);
                }

                ExecutionContext.Output($"##[group]Run {firstLine}");
                var multiLines = contents.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
                foreach (var line in multiLines)
                {
                    // Bright Cyan color
                    ExecutionContext.Output($"\x1b[36;1m{line}\x1b[0m");
                }
            }
            else
            {
                throw new InvalidOperationException($"Invalid action type {Action?.Type} for {nameof(ScriptHandler)}");
            }

            string argFormat;
            string shellCommand;
            string shellCommandPath = null;
            bool validateShellOnHost = !(StepHost is ContainerStepHost);
            string prependPath = string.Join(Path.PathSeparator.ToString(), ExecutionContext.Global.PrependPath.Reverse<string>());
            string shell = null;
            if (!Inputs.TryGetValue("shell", out shell) || string.IsNullOrEmpty(shell))
            {
                // TODO: figure out how defaults interact with template later
                // for now, we won't check job.defaults if we are inside a template.
                if (string.IsNullOrEmpty(ExecutionContext.ScopeName) && ExecutionContext.Global.JobDefaults.TryGetValue("run", out var runDefaults))
                {
                    runDefaults.TryGetValue("shell", out shell);
                }
            }
            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS
                shellCommand = "pwsh";
                if (validateShellOnHost)
                {
                    shellCommandPath = WhichUtil.Which(shellCommand, require: false, Trace, prependPath);
                    if (string.IsNullOrEmpty(shellCommandPath))
                    {
                        shellCommand = "powershell";
                        Trace.Info($"Defaulting to {shellCommand}");
                        shellCommandPath = WhichUtil.Which(shellCommand, require: true, Trace, prependPath);
                    }
                }
#else
                shellCommand = "sh";
                if (validateShellOnHost)
                {
                    shellCommandPath = WhichUtil.Which("bash", false, Trace, prependPath) ?? WhichUtil.Which("sh", true, Trace, prependPath);
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
                    shellCommandPath = WhichUtil.Which(parsed.shellCommand, true, Trace, prependPath);
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
            // Validate args
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            Inputs.TryGetValue("script", out var contents);
            contents = contents ?? string.Empty;

            string workingDirectory = null;
            if (!Inputs.TryGetValue("workingDirectory", out workingDirectory))
            {
                if (string.IsNullOrEmpty(ExecutionContext.ScopeName) && ExecutionContext.Global.JobDefaults.TryGetValue("run", out var runDefaults))
                {
                    if (runDefaults.TryGetValue("working-directory", out workingDirectory))
                    {
                        ExecutionContext.Debug("Overwrite 'working-directory' base on job defaults.");
                    }
                }
            }
            var workspaceDir = githubContext["workspace"] as StringContextData;
            workingDirectory = Path.Combine(workspaceDir, workingDirectory ?? string.Empty);

            string shell = null;
            if (!Inputs.TryGetValue("shell", out shell) || string.IsNullOrEmpty(shell))
            {
                if (string.IsNullOrEmpty(ExecutionContext.ScopeName) && ExecutionContext.Global.JobDefaults.TryGetValue("run", out var runDefaults))
                {
                    if (runDefaults.TryGetValue("shell", out shell))
                    {
                        ExecutionContext.Debug("Overwrite 'shell' base on job defaults.");
                    }
                }
            }

            var isContainerStepHost = StepHost is ContainerStepHost;

            string prependPath = string.Join(Path.PathSeparator.ToString(), ExecutionContext.Global.PrependPath.Reverse<string>());
            string commandPath, argFormat, shellCommand;
            // Set up default command and arguments
            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS
                shellCommand = "pwsh";
                commandPath = WhichUtil.Which(shellCommand, require: false, Trace, prependPath);
                if (string.IsNullOrEmpty(commandPath))
                {
                    shellCommand = "powershell";
                    Trace.Info($"Defaulting to {shellCommand}");
                    commandPath = WhichUtil.Which(shellCommand, require: true, Trace, prependPath);
                }
                ArgUtil.NotNullOrEmpty(commandPath, "Default Shell");
#else
                shellCommand = "sh";
                commandPath = WhichUtil.Which("bash", false, Trace, prependPath) ?? WhichUtil.Which("sh", true, Trace, prependPath);
#endif
                argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
            }
            else
            {
                var parsed = ScriptHandlerHelpers.ParseShellOptionString(shell);
                shellCommand = parsed.shellCommand;
                // For non-ContainerStepHost, the command must be located on the host by Which
                commandPath = WhichUtil.Which(parsed.shellCommand, !isContainerStepHost, Trace, prependPath);
                argFormat = $"{parsed.shellArgs}".TrimStart();
                if (string.IsNullOrEmpty(argFormat))
                {
                    argFormat = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellCommand);
                }
            }

            // Don't override runner telemetry here
            if (!string.IsNullOrEmpty(shellCommand) && IsActionStep)
            {
                ExecutionContext.StepTelemetry.Action = shellCommand;
            }

            // No arg format was given, shell must be a built-in
            if (string.IsNullOrEmpty(argFormat) || !argFormat.Contains("{0}"))
            {
                throw new ArgumentException("Invalid shell option. Shell must be a valid built-in (bash, sh, cmd, powershell, pwsh) or a format string containing '{0}'");
            }
            string scriptFilePath, resolvedScriptPath;
            if (IsActionStep)
            {
                // We do not not the full path until we know what shell is being used, so that we can determine the file extension
                scriptFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}{ScriptHandlerHelpers.GetScriptFileExtension(shellCommand)}");
                resolvedScriptPath = $"{StepHost.ResolvePathForStepHost(scriptFilePath).Replace("\"", "\\\"")}";
            }
            else
            {
                // JobExtensionRunners run a script file, we load that from the inputs here
                if (!Inputs.ContainsKey("path"))
                {
                    throw new ArgumentException("Expected 'path' input to be set");
                }
                scriptFilePath = Inputs["path"];
                ArgUtil.NotNullOrEmpty(scriptFilePath, "path");
                resolvedScriptPath = Inputs["path"].Replace("\"", "\\\"");
            }

            // Format arg string with script path
            var arguments = string.Format(argFormat, resolvedScriptPath);

            // Fix up and write the script
            contents = ScriptHandlerHelpers.FixUpScriptContents(shellCommand, contents);
#if OS_WINDOWS
            // Normalize Windows line endings
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            var encoding = ExecutionContext.Global.Variables.Retain_Default_Encoding && Console.InputEncoding.CodePage != 65001
                ? Console.InputEncoding
                : new UTF8Encoding(false);
#else
            // Don't add a BOM. It causes the script to fail on some operating systems (e.g. on Ubuntu 14).
            var encoding = new UTF8Encoding(false);
#endif            
            if (IsActionStep)
            {
                // Script is written to local path (ie host) but executed relative to the StepHost, which may be a container
                File.WriteAllText(scriptFilePath, contents, encoding);
            }

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
#if OS_OSX
            if (Environment.ContainsKey("DYLD_INSERT_LIBRARIES"))  // We don't check `isContainerStepHost` because we don't support container on macOS
            {
                // launch `node macOSRunInvoker.js shell args` instead of `shell args` to avoid macOS SIP remove `DYLD_INSERT_LIBRARIES` when launch process
                string node = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), NodeUtil.GetInternalNodeVersion(), "bin", $"node{IOUtil.ExeExtension}");
                string macOSRunInvoker = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), "macos-run-invoker.js");
                arguments = $"\"{macOSRunInvoker.Replace("\"", "\\\"")}\" \"{fileName.Replace("\"", "\\\"")}\" {arguments}";
                fileName = node;
            }
#endif
            var systemConnection = ExecutionContext.Global.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            if (systemConnection.Data.TryGetValue("GenerateIdTokenUrl", out var generateIdTokenUrl) && !string.IsNullOrEmpty(generateIdTokenUrl))
            {
                Environment["ACTIONS_ID_TOKEN_REQUEST_URL"] = generateIdTokenUrl;
                Environment["ACTIONS_ID_TOKEN_REQUEST_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            }

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
                                            inheritConsoleHandler: !ExecutionContext.Global.Variables.Retain_Default_Encoding,
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
