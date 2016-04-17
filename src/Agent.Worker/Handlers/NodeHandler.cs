using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(NodeHandler))]
    public interface INodeHandler : IHandler
    {
        NodeHandlerData Data { get; set; }
    }

    public sealed class NodeHandler : Handler, INodeHandler
    {
        public NodeHandlerData Data { get; set; }

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
            AddEndpointsToEnvironment();
            AddVariablesToEnvironment();

            // Resolve the target script.
            string target = Data.Target;
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(TaskDirectory, target);
            ArgUtil.File(target, nameof(target));

            // Resolve the working directory.
            string workingDirectory = Data.WorkingDirectory;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = TaskDirectory;
            }

            ArgUtil.Directory(workingDirectory, nameof(workingDirectory));

            // Setup the process invoker.
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OnDataReceived;
                processInvoker.ErrorDataReceived += OnDataReceived;
                string node = Path.Combine(
                    IOUtil.GetExternalsPath(),
                    "node",
                    "bin",
                    $"node{IOUtil.ExeExtension}");

                // Format the arguments passed to node.
                // 1) Wrap the script file path in double quotes.
                // 2) Escape double quotes within the script file path. Double-quote is a valid
                // file name character on Linux.
                string arguments = StringUtil.Format(@"""{0}""", target.Replace(@"""", @"\"""));

                // Execute the process. Exit code 0 should always be returned.
                // A non-zero exit code indicates infrastructural failure.
                // Task failure should be communicated over STDOUT using ## commands.
                await processInvoker.ExecuteAsync(
                    workingDirectory: workingDirectory,
                    fileName: node,
                    arguments: arguments,
                    environment: Environment,
                    requireExitCodeZero: true,
                    cancellationToken: ExecutionContext.CancellationToken);
            }
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!CommandHandler.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
