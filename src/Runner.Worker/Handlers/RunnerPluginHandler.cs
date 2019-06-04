using GitHub.Runner.Common.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(RunnerPluginHandler))]
    public interface IRunnerPluginHandler : IHandler
    {
        RunnerPluginHandlerData Data { get; set; }
    }

    public sealed class RunnerPluginHandler : Handler, IRunnerPluginHandler
    {
        public RunnerPluginHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.NotNullOrEmpty(Data.Target, nameof(Data.Target));

            // Update the env dictionary.
            AddPrependPathToEnvironment();

            // Make sure only particular task get run as runner plugin.
            var runnerPlugin = HostContext.GetService<IRunnerPluginManager>();
            ActionCommandManager.EnablePluginInternalCommand();
            try
            {
                await runnerPlugin.RunPluginActionAsync(ExecutionContext, Data.Target, Inputs, Environment, RuntimeVariables, OnDataReceived);
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
