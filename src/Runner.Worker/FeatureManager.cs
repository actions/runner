using System;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker
{
    public class FeatureManager
    {
        public static bool IsContainerHooksEnabled(Variables variables)
        {
            var isContainerHookFeatureFlagSet = variables?.GetBoolean(Constants.Runner.Features.AllowRunnerContainerHooks) ?? false;
            var isContainerHooksPathSet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath));
            return isContainerHookFeatureFlagSet && isContainerHooksPathSet;
        }
        public static bool IsStallDetectEnabled(Variables variables)
        {
            var isStallDetectFeatureFlagSet = variables?.GetBoolean(Constants.Runner.Features.AllowRunnerStallDetect) ?? false;
            return isStallDetectFeatureFlagSet;
        }
    }
}
