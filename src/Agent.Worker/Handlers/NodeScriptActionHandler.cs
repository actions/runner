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
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData;

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

            // Resolve the target script.
            string target = Data.Target;
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(TaskDirectory, target);
            ArgUtil.File(target, nameof(target));

            // Resolve the working directory.
            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            string workingDirectory = Data.WorkingDirectory;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = githubContext["workspace"] as StringContextData;
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
