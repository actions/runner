using Microsoft.VisualStudio.Services.Agent.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using System.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ShellHandler))]
    public interface IShellHandler : IHandler
    {
        ShellHandlerData Data { get; set; }
    }

    public sealed class ShellHandler : Handler, IShellHandler
    {
        public ShellHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            // get the entry executable shell
            // by default cmd on windows and bash on linux/mac
            // user can overwrite this
            string shell = Inputs.GetValueOrDefault("shell");
            if (string.IsNullOrEmpty(shell))
            {
#if OS_WINDOWS                
                // Resolve cmd.exe for windows
                shell = "cmd.exe";
#else
                shell = "bash";
#endif                
            }

            ArgUtil.NotNullOrEmpty(shell, nameof(shell));
            ExecutionContext.Output($"Use '{shell}' execute generated script.");

            // Write script to file
            string script = Inputs.GetValueOrDefault("script");
            string scriptFileExtension = null;
            if (shell.EndsWith("powershell", StringComparison.OrdinalIgnoreCase) ||
                shell.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase))
            {
                scriptFileExtension = "ps1";
            }
            else
            {
#if OS_WINDOWS
                scriptFileExtension = "cmd";
                script = "@echo off" + System.Environment.NewLine + script;
#else
                scriptFileExtension = "sh";
                script = "set -eo pipefail" + System.Environment.NewLine + script;
#endif
            }

            ExecutionContext.Output("Script contents:");
            ExecutionContext.Output(script);

            string scriptFile = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), $"{Guid.NewGuid()}.{scriptFileExtension}");

            ExecutionContext.Output($"Generate script file: {scriptFile}");
            File.WriteAllText(scriptFile, script, new UTF8Encoding(false));

            // get arguments
            // we support running cmd/bash/sh/powershell for now
            string arguments;
            if (scriptFileExtension.Equals("ps1", StringComparison.OrdinalIgnoreCase))
            {
                // powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command ". '<filePath>.Replace("'", "''"))'"
                arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command \". '{StepHost.ResolvePathForStepHost(scriptFile).Replace("'", "''")}'\"";
            }
            else if (scriptFileExtension.Equals("cmd", StringComparison.OrdinalIgnoreCase))
            {
                // cmd /D /E:ON /V:OFF /S /C "CALL "<filePath>""
                arguments = $"/D /E:ON /V:OFF /S /C \"CALL \"{StepHost.ResolvePathForStepHost(scriptFile)}\"\"";
            }
            else
            {
                // bash --noprofile --norc "<filePath>"
                arguments = $"--noprofile --norc \"{StepHost.ResolvePathForStepHost(scriptFile)}\"";
            }

            // get working directory
            string workingDirectory = Inputs.GetValueOrDefault("workingDirectory");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = ExecutionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);
            }

            // get environment variable
            AddPrependPathToEnvironment();

            // populate action environment variables.
            var selfRepo = ExecutionContext.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase) ||
                                                                     string.Equals(x.Alias, Pipelines.PipelineConstants.DesignerRepo, StringComparison.OrdinalIgnoreCase));

            // GITHUB_ACTOR=ericsciple
            Environment["GITHUB_ACTOR"] = selfRepo.Properties.Get<Pipelines.VersionInfo>(Pipelines.RepositoryPropertyNames.VersionInfo)?.Author ?? string.Empty;

            // GITHUB_REPOSITORY=bryanmacfarlane/actionstest
            Environment["GITHUB_REPOSITORY"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name, string.Empty);

            // GITHUB_WORKSPACE=/github/workspace
            Environment["GITHUB_WORKSPACE"] = ExecutionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);

            // GITHUB_SHA=1a204f473f6001b7fac9c6453e76702f689a41a9
            Environment["GITHUB_SHA"] = selfRepo.Version;

            // GITHUB_REF=refs/heads/master
            Environment["GITHUB_REF"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref, string.Empty);

            // GITHUB_TOKEN=TOKEN
            if (selfRepo.Endpoint != null)
            {
                var repoEndpoint = ExecutionContext.Endpoints.FirstOrDefault(x => x.Id == selfRepo.Endpoint.Id);
                if (repoEndpoint?.Authorization?.Parameters != null && repoEndpoint.Authorization.Parameters.ContainsKey("accessToken"))
                {
                    Environment["GITHUB_TOKEN"] = repoEndpoint.Authorization.Parameters["accessToken"];
                }
            }

            // HOME=/github/home
            // Environment["HOME"] = "/github/home";

            // GITHUB_WORKFLOW=test on push
            Environment["GITHUB_WORKFLOW"] = ExecutionContext.Variables.Build_DefinitionName;

            // GITHUB_EVENT_NAME=push
            Environment["GITHUB_EVENT_NAME"] = ExecutionContext.Variables.Get(BuildVariables.Reason);

            // GITHUB_ACTION=dump.env
            // GITHUB_EVENT_PATH=/github/workflow/event.json

            // execute through stephost
            StepHost.OutputDataReceived += OnDataReceived;
            StepHost.ErrorDataReceived += OnDataReceived;

#if OS_WINDOWS
            // It appears that node.exe outputs UTF8 when not in TTY mode.
            Encoding outputEncoding = Encoding.UTF8;
#else
            // Let .NET choose the default.
            Encoding outputEncoding = null;
#endif

            // Execute the process. Exit code 0 should always be returned.
            // A non-zero exit code indicates infrastructural failure.
            // Task failure should be communicated over STDOUT using ## commands.
            int exitCode = await StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                        fileName: StepHost.ResolvePathForStepHost(shell),
                                        arguments: arguments,
                                        environment: Environment,
                                        requireExitCodeZero: false,
                                        outputEncoding: outputEncoding,
                                        killProcessOnCancel: false,
                                        inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                        cancellationToken: ExecutionContext.CancellationToken);

            // Fail on non-zero exit code.
            if (exitCode != 0)
            {
                throw new Exception(StringUtil.Loc("ProcessCompletedWithExitCode0", exitCode));
            }
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!ActionCommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
