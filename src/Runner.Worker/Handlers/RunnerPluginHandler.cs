using System.Threading.Tasks;
using System;
using GitHub.Runner.Sdk;
using GitHub.Runner.Common;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(RunnerPluginHandler))]
    public interface IRunnerPluginHandler : IHandler
    {
        PluginActionExecutionData Data { get; set; }
    }

    public sealed class RunnerPluginHandler : Handler, IRunnerPluginHandler
    {
        public PluginActionExecutionData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.NotNullOrEmpty(Data.Plugin, nameof(Data.Plugin));

            // Update the env dictionary.
            AddPrependPathToEnvironment();

            // Make sure only particular task get run as runner plugin.
            var runnerPlugin = HostContext.GetService<IRunnerPluginManager>();
            ActionCommandManager.EnablePluginInternalCommand();
            try
            {
                await runnerPlugin.RunPluginActionAsync(ExecutionContext, Data.Plugin, Inputs, Environment, RuntimeVariables, OnDataReceived);
            }
            finally
            {
                ActionCommandManager.DisablePluginInternalCommand();
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
