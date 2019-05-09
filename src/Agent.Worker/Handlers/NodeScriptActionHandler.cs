using Microsoft.VisualStudio.Services.Agent.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(NodeScriptActionHandler))]
    public interface INodeScriptActionHandler : IHandler
    {
        NodeScriptActionHandlerData Data { get; set; }
    }

    public sealed class NodeScriptActionHandler : Handler, INodeScriptActionHandler
    {
        public NodeScriptActionHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(TaskDirectory, nameof(TaskDirectory));

            // Update the env dictionary.
            AddInputsToEnvironment();
            AddPrependPathToEnvironment();
            AddAgentVariablesToEnvironment();

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

            // GITHUB_EVENT_PATH=/github/workflow/event.Json

            // Resolve the target script.
            string target = Data.Target;
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(TaskDirectory, target);
            ArgUtil.File(target, nameof(target));

            // Resolve the working directory.
            string workingDirectory = Data.WorkingDirectory;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = ExecutionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                }
            }

            StepHost.OutputDataReceived += OnDataReceived;
            StepHost.ErrorDataReceived += OnDataReceived;

            string file;
            if (!string.IsNullOrEmpty(ExecutionContext.Container?.ContainerBringNodePath))
            {
                file = ExecutionContext.Container.ContainerBringNodePath;
            }
            else
            {
                file = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "node10", "bin", $"node{IOUtil.ExeExtension}");

            }

            // Format the arguments passed to node.
            // 1) Wrap the script file path in double quotes.
            // 2) Escape double quotes within the script file path. Double-quote is a valid
            // file name character on Linux.
            string arguments = StepHost.ResolvePathForStepHost(StringUtil.Format(@"""{0}""", target.Replace(@"""", @"\""")));

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
            Task step = StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                              fileName: StepHost.ResolvePathForStepHost(file),
                                              arguments: arguments,
                                              environment: Environment,
                                              requireExitCodeZero: true,
                                              outputEncoding: outputEncoding,
                                              killProcessOnCancel: false,
                                              inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                              cancellationToken: ExecutionContext.CancellationToken);

            // Wait for either the node exit or force finish through ##vso command
            await System.Threading.Tasks.Task.WhenAny(step, ExecutionContext.ForceCompleted);

            if (ExecutionContext.ForceCompleted.IsCompleted)
            {
                ExecutionContext.Debug("The task was marked as \"done\", but the process has not closed after 5 seconds. Treating the task as complete.");
            }
            else
            {
                await step;
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
