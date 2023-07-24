using System;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
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
            // Set extra telemetry base on the current context.
            ExecutionContext.StepTelemetry.Type = plugin;

            // Update the env dictionary.
            AddPrependPathToEnvironment();

            // Make sure only particular task get run as runner plugin.
            var runnerPlugin = HostContext.GetService<IRunnerPluginManager>();
            StallManager stallManager = FeatureManager.IsStallDetectEnabled(ExecutionContext.Global.Variables) ? new StallManager(ExecutionContext) : null;

            using (OutputManager outputManager = new OutputManager(ExecutionContext, ActionCommandManager, null, stallManager))
            {
                ActionCommandManager.EnablePluginInternalCommand();
                try
                {
                    stallManager?.Initialize();
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
