using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Build = Microsoft.TeamFoundation.Build.WebApi;
using DistributedTask = Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ScriptHandler))]
    public interface IScriptHandler : IHandler
    {
    }

    public sealed class ScriptHandler : Handler, IScriptHandler
    {
        public async Task RunAsync()
        {
            // Validate args
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            Inputs.TryGetValue("script", out var contents);
            contents = contents ?? string.Empty;

            Inputs.TryGetValue("workingDirectory", out var workingDirectory);
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = ExecutionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                }
            }

#if OS_WINDOWS
            // Fixup contents
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            // Note, use @echo off instead of using the /Q command line switch.
            // When /Q is used, echo can't be turned on.
            contents = $"@echo off\r\n{contents}";

            // Write the script
            var filePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.cmd");
            var encoding = ExecutionContext.Variables.Retain_Default_Encoding && Console.InputEncoding.CodePage != 65001
                ? Console.InputEncoding
                : new UTF8Encoding(false);
            File.WriteAllText(filePath, contents, encoding);

            // Command path
            var commandPath = System.Environment.GetEnvironmentVariable("ComSpec");
            ArgUtil.NotNullOrEmpty(commandPath, "%ComSpec%");

            // Arguments
            var arguments = $"/D /E:ON /V:OFF /S /C \"CALL \"{StepHost.ResolvePathForStepHost(filePath)}\"\"";
#else
            // Fixup contents
            contents = $"set -eo pipefail\n{contents}";

            // Write the script
            var filePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.sh");
            // Don't add a BOM. It causes the script to fail on some operating systems (e.g. on Ubuntu 14).
            File.WriteAllText(filePath, contents, new UTF8Encoding(false));

            // Command path
            var commandPath = WhichUtil.Which("bash") ?? WhichUtil.Which("sh", true);

            // Arguments
            var arguments = $"--noprofile --norc {StepHost.ResolvePathForStepHost(filePath).Replace("\"", "\\\"")}";
#endif

            ExecutionContext.Output("Script contents:");
            ExecutionContext.Output(contents);
            ExecutionContext.Output("========================== Starting Command Output ===========================");

            // Prepend PATH
            AddPrependPathToEnvironment();

            // Populate action environment variables
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
            Environment["GITHUB_EVENT_NAME"] = ExecutionContext.Variables.Get(Build::BuildVariables.Reason);

            // GITHUB_ACTION=dump.env
            // GITHUB_EVENT_PATH=/github/workflow/event.json

            // Execute through stephost
            StepHost.OutputDataReceived += OnDataReceived;
            StepHost.ErrorDataReceived += OnDataReceived;

            // Execute
            int exitCode = await StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                        fileName: StepHost.ResolvePathForStepHost(commandPath),
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
