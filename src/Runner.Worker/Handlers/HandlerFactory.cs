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
            string actionDirectory,
            List<JobExtensionRunner> localActionContainerSetupSteps);
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
            string actionDirectory,
            List<JobExtensionRunner> localActionContainerSetupSteps)
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
                var nodeData = data as NodeJSActionExecutionData;

                // With node12 EoL in 04/2022, we want to be able to uniformly upgrade all JS actions to node16 from the server
                if (executionContext.Global.Variables.GetBoolean("DistributedTask.ForceGithubJavascriptActionsToNode16") ?? false)
                {
                    // The user can opt out of this behaviour by setting this variable to true, either setting 'env' in their workflow or as an environment variable on their machine
                    executionContext.Global.EnvironmentVariables.TryGetValue(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, out var workflowOptOut);
                    var isWorkflowOptOutSet = string.IsNullOrEmpty(workflowOptOut);
                    if (isWorkflowOptOutSet)
                    {
                        // Fallback to environment variable opt out
                        var isLocalEnvOptOut = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion));
                        if (!isLocalEnvOptOut)
                        {
                            nodeData.Script = "node16";
                        }
                    }
                    else
                    {
                        var isWorkflowOptOut = StringUtil.ConvertToBoolean(workflowOptOut);
                        if (!isWorkflowOptOut)
                        {
                            nodeData.Script = "node16";
                        }
                    }
                }
                (handler as INodeScriptActionHandler).Data = nodeData;
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
            handler.LocalActionContainerSetupSteps = localActionContainerSetupSteps;
            return handler;
        }
    }
}
