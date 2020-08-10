using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(HandlerFactory))]
    public interface IHandlerFactory : IRunnerService
    {
        IHandler Create(
            IExecutionContext executionContext,
            Pipelines.ActionStepDefinitionReference action,
            IStepHost stepHost,
            ActionExecutionData data,
            Dictionary<string, string> inputs,
            Dictionary<string, string> environment,
            Variables runtimeVariables,
            string actionDirectory);
    }

    public sealed class HandlerFactory : RunnerService, IHandlerFactory
    {
        public IHandler Create(
            IExecutionContext executionContext,
            Pipelines.ActionStepDefinitionReference action,
            IStepHost stepHost,
            ActionExecutionData data,
            Dictionary<string, string> inputs,
            Dictionary<string, string> environment,
            Variables runtimeVariables,
            string actionDirectory)
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
            if (data.ExecutionType == ActionExecutionType.Container)
            {
                handler = HostContext.CreateService<IContainerActionHandler>();
                (handler as IContainerActionHandler).Data = data as ContainerActionExecutionData;
            }
            else if (data.ExecutionType == ActionExecutionType.NodeJS)
            {
                handler = HostContext.CreateService<INodeScriptActionHandler>();
                (handler as INodeScriptActionHandler).Data = data as NodeJSActionExecutionData;
            }
            else if (data.ExecutionType == ActionExecutionType.Script)
            {
                handler = HostContext.CreateService<IScriptHandler>();
                (handler as IScriptHandler).Data = data as ScriptActionExecutionData;
            }
            else if (data.ExecutionType == ActionExecutionType.Plugin)
            {
                // Runner plugin
                handler = HostContext.CreateService<IRunnerPluginHandler>();
                (handler as IRunnerPluginHandler).Data = data as PluginActionExecutionData;
            }
            else if (data.ExecutionType == ActionExecutionType.Composite)
            {
                handler = HostContext.CreateService<ICompositeActionHandler>();
                (handler as ICompositeActionHandler).Data = data as CompositeActionExecutionData;
            }
            else
            {
                // This should never happen.
                throw new NotSupportedException(data.ExecutionType.ToString());
            }

            handler.Action = action;
            handler.Environment = environment;
            handler.RuntimeVariables = runtimeVariables;
            handler.ExecutionContext = executionContext;
            handler.StepHost = stepHost;
            handler.Inputs = inputs;
            handler.ActionDirectory = actionDirectory;
            return handler;
        }
    }
}
