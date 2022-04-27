using System;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    public class FeatureFlagManager
    {
        public static bool IsHookFeatureEnabled(IExecutionContext executionContext) 
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath)); // TODO: renenable feature flag before merge, &&
                    // ((executionContext.Global.Variables.GetBoolean(Constants.Runner.Features.AllowRunnerContainerHooks)) ?? true);
        }
    }
}
