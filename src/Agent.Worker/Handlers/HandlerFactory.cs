using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(HandlerFactory))]
    public interface IHandlerFactory : IAgentService
    {
        IHandler Create(
            IExecutionContext executionContext,
            Pipelines.ActionStepDefinitionReference action,
            IStepHost stepHost,
            HandlerData data,
            Dictionary<string, string> inputs,
            Dictionary<string, string> environment,
            Variables runtimeVariables,
            string taskDirectory);
    }

    public sealed class HandlerFactory : AgentService, IHandlerFactory
    {
        public IHandler Create(
            IExecutionContext executionContext,
            Pipelines.ActionStepDefinitionReference action,
            IStepHost stepHost,
            HandlerData data,
            Dictionary<string, string> inputs,
            Dictionary<string, string> environment,
            Variables runtimeVariables,
            string taskDirectory)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(stepHost, nameof(stepHost));
            ArgUtil.NotNull(data, nameof(data));
            ArgUtil.NotNull(inputs, nameof(inputs));
            ArgUtil.NotNull(environment, nameof(environment));
            ArgUtil.NotNull(runtimeVariables, nameof(runtimeVariables));

            // Create the handler.
            IHandler handler;
            if (data is ContainerActionHandlerData)
            {
                handler = HostContext.CreateService<IContainerActionHandler>();
                (handler as IContainerActionHandler).Data = data as ContainerActionHandlerData;
            }
            else if (data is NodeScriptActionHandlerData)
            {
                handler = HostContext.CreateService<INodeScriptActionHandler>();
                (handler as INodeScriptActionHandler).Data = data as NodeScriptActionHandlerData;
            }
            else if (data is ScriptActionHandlerData)
            {
                handler = HostContext.CreateService<IScriptHandler>();
                (handler as IScriptHandler).Data = data as ScriptActionHandlerData;
            }
            else if (data is AgentPluginHandlerData)
            {
                // Agent plugin
                handler = HostContext.CreateService<IAgentPluginHandler>();
                (handler as IAgentPluginHandler).Data = data as AgentPluginHandlerData;
            }
            else
            {
                // This should never happen.
                throw new NotSupportedException();
            }

            handler.Environment = environment;
            handler.RuntimeVariables = runtimeVariables;
            handler.ExecutionContext = executionContext;
            handler.StepHost = stepHost;
            handler.Inputs = inputs;
            handler.TaskDirectory = taskDirectory;
            return handler;
        }
    }
}