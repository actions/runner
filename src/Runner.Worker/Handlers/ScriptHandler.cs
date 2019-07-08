using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ScriptHandler))]
    public interface IScriptHandler : IHandler
    {
        ScriptActionHandlerData Data { get; set; }
    }

    public sealed class ScriptHandler : Handler, IScriptHandler
    {
        public ScriptActionHandlerData Data { get; set; }

        public async Task RunAsync()
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

            Inputs.TryGetValue("workingDirectory", out var workingDirectory);
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = githubContext["workspace"] as StringContextData;
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                }
            }

            Inputs.TryGetValue("shell", out var shell);

            var scriptFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}");
            var resolvedPath = StepHost.ResolvePathForStepHost(scriptFilePath).Replace("\"", "\\\"");
            string commandPath, arguments, shellName;
#if OS_WINDOWS
            // Fixup contents
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            var encoding = ExecutionContext.Variables.Retain_Default_Encoding && Console.InputEncoding.CodePage != 65001
                ? Console.InputEncoding
                : new UTF8Encoding(false);

            // Set up default command and arguments
            if (string.IsNullOrEmpty(shell))
            {
                shellName = "cmd";

                // Note, use @echo off instead of using the /Q command line switch.
                // When /Q is used, echo can't be turned on.
                contents = $"@echo off\r\n{contents}";

                commandPath = System.Environment.GetEnvironmentVariable("ComSpec");
                ArgUtil.NotNullOrEmpty(commandPath, "%ComSpec%");

                arguments = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellName);
            }
#else
            // Don't add a BOM. It causes the script to fail on some operating systems (e.g. on Ubuntu 14).
            var encoding = new UTF8Encoding(false);

            // Set up command and arguments
            if (string.IsNullOrEmpty(shell))
            {
                shellName = "sh";

                // Fixup default contents
                contents = $"set -eo pipefail\n{contents}";

                commandPath = WhichUtil.Which("bash") ?? WhichUtil.Which("sh", true);

                arguments = ScriptHandlerHelpers.GetScriptArgumentsFormat(shellName);
            }
#endif
            else
            {
                var parsed = ParseShellOptionString(shell);
                shellName = parsed.shellCommand;
                commandPath = WhichUtil.Which(parsed.shellCommand, true);
                arguments = $"{parsed.shellArgs}".TrimStart();
            }

            // We do not not the full path until we know what shell is being used, so that we can determine the file extension
            var fullyResolvedPath = $"{resolvedPath}{ScriptHandlerHelpers.GetScriptFileExtension(shellName)}";

            // [...args] were given in shell options, or system default was used
            if (!string.IsNullOrEmpty(arguments))
            {
                arguments = FormatArgumentString(arguments, fullyResolvedPath);
            }
            // if no [...args] in shell options, look up defaults, finally defaulting to 'command scriptfile'
            else
            {
                arguments = $"{fullyResolvedPath}";
            }

            // Write the script
            File.WriteAllText(fullyResolvedPath, contents, encoding);

            ExecutionContext.Output("Script contents:");
            ExecutionContext.Output(contents);
            ExecutionContext.Output("========================== Starting Command Output ===========================");

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
            var fileName = StepHost.ResolvePathForStepHost(commandPath);
            ExecutionContext.Command($"{fileName} {arguments}");

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
                    ExecutionContext.Error(StringUtil.Loc("ProcessCompletedWithExitCode0", exitCode));
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }
        }

        private string FormatArgumentString(string argString, string scriptFilePath)
        {
            // No args givin in options and no defaults exist. Only arg is the script file
            if (string.IsNullOrEmpty(argString))
            {
                return scriptFilePath;
            }
            else
            {
                // Format string, e.g. `-args {0}` may be given in options or come from system defaults
                if (argString.Contains("{0}"))
                {
                    return string.Format(argString, scriptFilePath);
                }
                // Regular arg string, e.g. `-abc file.sh` 
                return $"{argString} {scriptFilePath}";
            }
        }

        private (string shellCommand, string shellArgs) ParseShellOptionString(string shellOption)
        {
            var shellStringParts = shellOption.Split(" ", 2);
            if (shellStringParts.Length == 2)
            {
                return (shellCommand: shellStringParts[0], shellArgs: $"{shellStringParts[1]}");
            }
            else if (shellStringParts.Length == 1)
            {
                return (shellCommand: shellStringParts[0], shellArgs: "");
            }
            else
            {
                // TODO error handling
                // ... failed to parse COMMAND [..ARGS] from {string}
                return (shellCommand: "", shellArgs: "");
            }
        }
    }
}
