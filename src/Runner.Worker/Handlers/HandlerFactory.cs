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

                // With node12 EoL in 04/2022 and node16 EoL in 09/23, we want to execute all JS actions using node20
                // With node20 EoL approaching, we're preparing to migrate to node24
                if (string.Equals(nodeData.NodeVersion, "node12", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(nodeData.NodeVersion, "node16", StringComparison.InvariantCultureIgnoreCase))
                {
                    nodeData.NodeVersion = Common.Constants.Runner.NodeMigration.Node20;
                }

                // Check if node20 was explicitly specified in the action
                // We don't modify if node24 was explicitly specified
                if (string.Equals(nodeData.NodeVersion, Constants.Runner.NodeMigration.Node20, StringComparison.InvariantCultureIgnoreCase))
                {
                    bool useNode24ByDefault = FeatureManager.IsUseNode24ByDefaultEnabled(executionContext.Global.Variables);
                    bool requireNode24 = FeatureManager.IsRequireNode24Enabled(executionContext.Global.Variables);

                    var (nodeVersion, configWarningMessage) = NodeUtil.DetermineActionsNodeVersion(environment, useNode24ByDefault, requireNode24);
                    var (finalNodeVersion, platformWarningMessage) = NodeUtil.CheckNodeVersionForLinuxArm32(nodeVersion);
                    nodeData.NodeVersion = finalNodeVersion;

                    if (!string.IsNullOrEmpty(configWarningMessage))
                    {
                        executionContext.Warning(configWarningMessage);
                    }

                    if (!string.IsNullOrEmpty(platformWarningMessage))
                    {
                        executionContext.Warning(platformWarningMessage);
                    }

                    // Show information about Node 24 migration in Phase 2
                    if (useNode24ByDefault && !requireNode24 && string.Equals(finalNodeVersion, Constants.Runner.NodeMigration.Node24, StringComparison.OrdinalIgnoreCase))
                    {
                        string infoMessage = "Node 20 is being deprecated. This workflow is running with Node 24 by default. " +
                                             "If you need to temporarily use Node 20, you can set the ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION=true environment variable.";
                        executionContext.Output(infoMessage);
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
