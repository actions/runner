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

        public static bool IsNode24Enabled(Variables variables)
        {
            bool isEnabled = variables?.GetBoolean(Constants.Runner.Features.UseNode24) ?? false;
            
            if (!isEnabled)
            {
                var envValue = Environment.GetEnvironmentVariable(Constants.Runner.Features.UseNode24) ?? 
                               Environment.GetEnvironmentVariable("RUNNER_USENODE24") ??
                               Environment.GetEnvironmentVariable("runner_usenode24");
                
                isEnabled = !string.IsNullOrEmpty(envValue) && 
                           (string.Equals(envValue, "true", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(envValue, "1", StringComparison.OrdinalIgnoreCase));
            }
            
            return isEnabled;
        }
    }
}
