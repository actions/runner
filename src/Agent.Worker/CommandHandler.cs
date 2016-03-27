using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(CommandHandler))]
    public interface ICommandHandler : IAgentService
    {
        bool TryProcessCommand(IExecutionContext context, string input);
    }

    public class CommandHandler : AgentService, ICommandHandler
    {
        private readonly Dictionary<String, ICommandExtension> _commandHandlers = new Dictionary<String, ICommandExtension>(StringComparer.OrdinalIgnoreCase);
        private readonly object _commandSerializeLock = new object();

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            // Register all command extensions
            var extensionManager = hostContext.GetService<IExtensionManager>();
            foreach (var commandExt in extensionManager.GetExtensions<ICommandExtension>() ?? new List<ICommandExtension>())
            {
                Trace.Info($"Register command extension for area {commandExt.CommandArea}");
                _commandHandlers[commandExt.CommandArea] = commandExt;
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
                    context.Warning($"'{input}' contains logging command keyword '##vso'. TODO: aka link to command DOC.");
                }

                return false;
            }

            Trace.Info($"Process logging commnad ##vso[{command.Area}.{command.Event}]");
            ICommandExtension handler;
            if (_commandHandlers.TryGetValue(command.Area, out handler))
            {
                // process logging command in serialize oreder.
                lock(_commandSerializeLock)
                {
                    try
                    {
                        handler.ProcessCommand(context, command);
                        context.Debug($"Processed logging command: {input}");
                    }
                    catch (Exception ex)
                    {
                        context.Error(ex);
                        context.Error($"Unable to process command {command} successfully.");
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
}
