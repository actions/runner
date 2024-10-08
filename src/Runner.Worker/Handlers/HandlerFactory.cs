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
                if (string.Equals(nodeData.NodeVersion, "node12", StringComparison.InvariantCultureIgnoreCase))
                {
                    var repoAction = action as Pipelines.RepositoryPathReference;
                    if (repoAction != null)
                    {
                        var warningActions = new HashSet<string>();
                        if (executionContext.Global.Variables.TryGetValue(Constants.Runner.EnforcedNode12DetectedAfterEndOfLifeEnvVariable, out var node16ForceWarnings))
                        {
                            warningActions = StringUtil.ConvertFromJson<HashSet<string>>(node16ForceWarnings);
                        }

                        string repoActionFullName;
                        if (string.IsNullOrEmpty(repoAction.Name))
                        {
                            repoActionFullName = repoAction.Path; // local actions don't have a 'Name'
                        }
                        else
                        {
                            repoActionFullName = $"{repoAction.Name}/{repoAction.Path ?? string.Empty}".TrimEnd('/') + $"@{repoAction.Ref}";
                        }

                        warningActions.Add(repoActionFullName);
                        executionContext.Global.Variables.Set("Node16ForceActionsWarnings", StringUtil.ConvertToJson(warningActions));
                    }
                    nodeData.NodeVersion = "node16";
                }

                var localForceActionsToNode20 = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(Constants.Variables.Agent.ManualForceActionsToNode20));
                executionContext.Global.EnvironmentVariables.TryGetValue(Constants.Variables.Actions.ManualForceActionsToNode20, out var workflowForceActionsToNode20);
                var enforceNode20Locally = !string.IsNullOrWhiteSpace(workflowForceActionsToNode20) ? StringUtil.ConvertToBoolean(workflowForceActionsToNode20) : localForceActionsToNode20;
                if (string.Equals(nodeData.NodeVersion, "node16")
                && ((executionContext.Global.Variables.GetBoolean("DistributedTask.ForceGithubJavascriptActionsToNode20") ?? false) || enforceNode20Locally))
                {
                    executionContext.Global.EnvironmentVariables.TryGetValue(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, out var workflowOptOut);
                    var isWorkflowOptOutSet = !string.IsNullOrWhiteSpace(workflowOptOut);
                    var isLocalOptOut = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion));
                    bool isOptOut = isWorkflowOptOutSet ? StringUtil.ConvertToBoolean(workflowOptOut) : isLocalOptOut;

                    if (isOptOut && (executionContext.Global.Variables.GetBoolean("DistributedTask.NotAllowOptOutForNode20") ?? false))
                    {
                        executionContext.Global.JobTelemetry.Add(new JobTelemetry()
                        {
                            Type = JobTelemetryType.General,
                            Message = $"Not allowing opt out for node20 in step {executionContext.Id}"
                        });
                        Trace.Info("Not allowing opt out for node20");
                        executionContext.Warning("End of life for Actions Node16. For more info: https://github.blog/changelog/2024-09-25-end-of-life-for-actions-node16/");
                        isOptOut = false;
                    }

                    if (!isOptOut)
                    {
                        var repoAction = action as Pipelines.RepositoryPathReference;
                        if (repoAction != null)
                        {
                            var warningActions = new HashSet<string>();
                            if (executionContext.Global.Variables.TryGetValue(Constants.Runner.EnforcedNode16DetectedAfterEndOfLifeEnvVariable, out var node20ForceWarnings))
                            {
                                warningActions = StringUtil.ConvertFromJson<HashSet<string>>(node20ForceWarnings);
                            }

                            string repoActionFullName;
                            if (string.IsNullOrEmpty(repoAction.Name))
                            {
                                repoActionFullName = repoAction.Path; // local actions don't have a 'Name'
                            }
                            else
                            {
                                repoActionFullName = $"{repoAction.Name}/{repoAction.Path ?? string.Empty}".TrimEnd('/') + $"@{repoAction.Ref}";
                            }

                            warningActions.Add(repoActionFullName);
                            executionContext.Global.Variables.Set(Constants.Runner.EnforcedNode16DetectedAfterEndOfLifeEnvVariable, StringUtil.ConvertToJson(warningActions));
                        }
                        nodeData.NodeVersion = "node20";
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
