using System;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    public class FeatureFlagManager
    {
        public static bool IsContainerHooksEnabled(IExecutionContext executionContext)
        {
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath)) && 
            ((executionContext.Global.Variables.GetBoolean(Constants.Runner.Features.AllowRunnerContainerHooks)) ?? false);
        }
    }
}
