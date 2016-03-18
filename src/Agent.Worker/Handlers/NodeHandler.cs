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
            ArgUtil.NotNull(TaskDirectory, nameof(TaskDirectory));

            // Update the env dictionary.
            AddInputsToEnvironment();
            // TODO: What about for ad-hoc scripts? Should the system endpoint not be added?
            AddEndpointsToEnvironment();
            AddVariablesToEnvironment();

            // Resolve the target script.
            string target = Data.Target;
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(TaskDirectory, target);
            if (!File.Exists(target))
            {
                throw new Exception("TODO: task script file not found message.");
            }

            // Resolve the working directory.
            string workingDirectory = Data.WorkingDirectory;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = TaskDirectory;
            }
            else if (!Directory.Exists(workingDirectory))
            {
                throw new Exception("TODO: task working directory not exist message");
            }

            // Setup the process invoker.
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                object outputLock = new object();
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
                int exitCode = await processInvoker.ExecuteAsync(
                    workingDirectory: workingDirectory,
                    fileName: node,
                    arguments: arguments,
                    environment: Environment,
                    cancellationToken: ExecutionContext.CancellationToken);
                if (exitCode != 0)
                {
                    throw new Exception("TODO: BETTER ERROR MESSAGE");
                }
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
