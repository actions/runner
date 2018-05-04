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

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(AgentPluginHandler))]
    public interface IAgentPluginHandler : IHandler
    {
        AgentPluginHandlerData Data { get; set; }
    }

    public sealed class AgentPluginHandler : Handler, IAgentPluginHandler
    {
        public AgentPluginHandlerData Data { get; set; }

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

            var agentPlugin = HostContext.GetService<IAgentPluginManager>();
            await agentPlugin.RunPluginTaskAsync(ExecutionContext, Data.Target, Inputs, Environment, Data.Stage, OnDataReceived);
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!CommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
