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
        internal static bool ShouldTrackAsArm32Node20(bool deprecateArm32, string preferredNodeVersion, string finalNodeVersion, string platformWarningMessage)
        {
            return deprecateArm32 &&
                !string.IsNullOrEmpty(platformWarningMessage) &&
                string.Equals(preferredNodeVersion, Constants.Runner.NodeMigration.Node24, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(finalNodeVersion, Constants.Runner.NodeMigration.Node20, StringComparison.OrdinalIgnoreCase);
        }

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

                // Read flags early; actionName is also resolved up front for tracking after version is determined
                bool warnOnNode20 = executionContext.Global.Variables?.GetBoolean(Constants.Runner.NodeMigration.WarnOnNode20Flag) ?? false;
                bool deprecateArm32 = executionContext.Global.Variables?.GetBoolean(Constants.Runner.NodeMigration.DeprecateLinuxArm32Flag) ?? false;
                bool killArm32 = executionContext.Global.Variables?.GetBoolean(Constants.Runner.NodeMigration.KillLinuxArm32Flag) ?? false;
                string node20RemovalDate = executionContext.Global.Variables?.Get(Constants.Runner.NodeMigration.Node20RemovalDateVariable);
                string actionName = GetActionName(action);

                // Check if node20 was explicitly specified in the action
                // We don't modify if node24 was explicitly specified
                if (string.Equals(nodeData.NodeVersion, Constants.Runner.NodeMigration.Node20, StringComparison.InvariantCultureIgnoreCase))
                {
                    bool useNode24ByDefault = executionContext.Global.Variables?.GetBoolean(Constants.Runner.NodeMigration.UseNode24ByDefaultFlag) ?? false;
                    bool requireNode24 = executionContext.Global.Variables?.GetBoolean(Constants.Runner.NodeMigration.RequireNode24Flag) ?? false;

                    var (nodeVersion, configWarningMessage) = NodeUtil.DetermineActionsNodeVersion(environment, useNode24ByDefault, requireNode24);
                    var (finalNodeVersion, platformWarningMessage) = NodeUtil.CheckNodeVersionForLinuxArm32(nodeVersion, deprecateArm32, killArm32, node20RemovalDate);

                    // ARM32 kill switch: fail the step
                    if (finalNodeVersion == null)
                    {
                        executionContext.Error(platformWarningMessage);
                        throw new InvalidOperationException(platformWarningMessage);
                    }

                    nodeData.NodeVersion = finalNodeVersion;

                    if (!string.IsNullOrEmpty(configWarningMessage))
                    {
                        executionContext.Warning(configWarningMessage);
                    }

                    if (!string.IsNullOrEmpty(platformWarningMessage))
                    {
                        executionContext.Warning(platformWarningMessage);
                    }

                    // Track actions based on their final node version
                    if (!string.IsNullOrEmpty(actionName))
                    {
                        if (string.Equals(finalNodeVersion, Constants.Runner.NodeMigration.Node24, StringComparison.OrdinalIgnoreCase))
                        {
                            // Action was upgraded from node20 to node24
                            executionContext.Global.UpgradedToNode24Actions?.Add(actionName);
                        }
                        else if (ShouldTrackAsArm32Node20(deprecateArm32, nodeVersion, finalNodeVersion, platformWarningMessage))
                        {
                            // Action is on node20 because ARM32 can't run node24
                            executionContext.Global.Arm32Node20Actions?.Add(actionName);
                        }
                        else if (warnOnNode20)
                        {
                            // Action is still running on node20 (general case)
                            executionContext.Global.DeprecatedNode20Actions?.Add(actionName);
                        }
                    }

                    // Show information about Node 24 migration in Phase 2
                    if (useNode24ByDefault && !requireNode24 && string.Equals(finalNodeVersion, Constants.Runner.NodeMigration.Node24, StringComparison.OrdinalIgnoreCase))
                    {
                        string infoMessage = "Node 20 is being deprecated. This workflow is running with Node 24 by default. " +
                                             "If you need to temporarily use Node 20, you can set the ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION=true environment variable. " +
                                             $"For more information see: {Constants.Runner.NodeMigration.Node20DeprecationUrl}";
                        executionContext.Output(infoMessage);
                    }
                }
                else if (string.Equals(nodeData.NodeVersion, Constants.Runner.NodeMigration.Node24, StringComparison.InvariantCultureIgnoreCase))
                {
                    var (finalNodeVersion, platformWarningMessage) = NodeUtil.CheckNodeVersionForLinuxArm32(nodeData.NodeVersion, deprecateArm32, killArm32, node20RemovalDate);

                    // ARM32 kill switch: fail the step
                    if (finalNodeVersion == null)
                    {
                        executionContext.Error(platformWarningMessage);
                        throw new InvalidOperationException(platformWarningMessage);
                    }

                    var preferredVersion = nodeData.NodeVersion;
                    nodeData.NodeVersion = finalNodeVersion;

                    if (!string.IsNullOrEmpty(platformWarningMessage))
                    {
                        executionContext.Warning(platformWarningMessage);
                    }

                    if (!string.IsNullOrEmpty(actionName) && ShouldTrackAsArm32Node20(deprecateArm32, preferredVersion, finalNodeVersion, platformWarningMessage))
                    {
                        executionContext.Global.Arm32Node20Actions?.Add(actionName);
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

        private static string GetActionName(Pipelines.ActionStepDefinitionReference action)
        {
            if (action is Pipelines.RepositoryPathReference repoRef)
            {
                var pathString = string.Empty;
                if (!string.IsNullOrEmpty(repoRef.Path))
                {
                    pathString = string.IsNullOrEmpty(repoRef.Name)
                        ? repoRef.Path
                        : $"/{repoRef.Path}";
                }
                var repoString = string.IsNullOrEmpty(repoRef.Ref)
                    ? $"{repoRef.Name}{pathString}"
                    : $"{repoRef.Name}{pathString}@{repoRef.Ref}";
                return string.IsNullOrEmpty(repoString) ? null : repoString;
            }

            return null;
        }
    }
}
