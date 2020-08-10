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

        public async Task RunAsync(ActionRunStage stage)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            string plugin = null;
            if (stage == ActionRunStage.Main)
            {
                plugin = Data.Plugin;
            }
            else if (stage == ActionRunStage.Post)
            {
                plugin = Data.Post;
            }

            ArgUtil.NotNullOrEmpty(plugin, nameof(plugin));

            // Update the env dictionary.
            AddPrependPathToEnvironment();

            // Make sure only particular task get run as runner plugin.
            var runnerPlugin = HostContext.GetService<IRunnerPluginManager>();
            using (var outputManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                ActionCommandManager.EnablePluginInternalCommand();
                try
                {
                    await runnerPlugin.RunPluginActionAsync(ExecutionContext, plugin, Inputs, Environment, RuntimeVariables, outputManager.OnDataReceived);
                }
                finally
                {
                    ActionCommandManager.DisablePluginInternalCommand();
                }
            }
        }
    }
}
