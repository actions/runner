using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(WorkerCommandManager))]
    public interface IWorkerCommandManager : IAgentService
    {
        bool TryProcessCommand(IExecutionContext context, string input);
    }

    public sealed class WorkerCommandManager : AgentService, IWorkerCommandManager
    {
        private readonly Dictionary<string, IWorkerCommandExtension> _commandExtensions = new Dictionary<string, IWorkerCommandExtension>(StringComparer.OrdinalIgnoreCase);
        private readonly object _commandSerializeLock = new object();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            // Register all command extensions
            var extensionManager = hostContext.GetService<IExtensionManager>();
            foreach (var commandExt in extensionManager.GetExtensions<IWorkerCommandExtension>() ?? new List<IWorkerCommandExtension>())
            {
                Trace.Info($"Register command extension for area {commandExt.CommandArea}");
                _commandExtensions[commandExt.CommandArea] = commandExt;
            }
        }

        public bool TryProcessCommand(IExecutionContext context, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // TryParse input to Command
            Command command;
            if (!Command.TryParse(input, out command))
            {
                // if parse fail but input contains ##vso, print warning with DOC link
                if (input.IndexOf("##vso") >= 0)
                {
                    context.Warning(StringUtil.Loc("CommandKeywordDetected", input));
                }

                return false;
            }

            IWorkerCommandExtension extension;
            if (_commandExtensions.TryGetValue(command.Area, out extension))
            {
                // process logging command in serialize oreder.
                lock (_commandSerializeLock)
                {
                    try
                    {
                        context.Debug($"Processing: {input}");
                        extension.ProcessCommand(context, command);
                    }
                    catch (Exception ex)
                    {
                        context.Error(StringUtil.Loc("CommandProcessFailed", input));
                        context.Error(ex);
                        context.CommandResult = TaskResult.Failed;
                    }
                }
            }
            else
            {
                context.Warning(StringUtil.Loc("CommandNotFound", command.Area));
            }

            return true;
        }
    }

    public interface IWorkerCommandExtension : IExtension
    {
        string CommandArea { get; }

        void ProcessCommand(IExecutionContext context, Command command);
    }
}
