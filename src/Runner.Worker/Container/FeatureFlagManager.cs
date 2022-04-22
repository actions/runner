using System;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    public class FeatureFlagManager
    {
        public static bool IsHookFeatureEnabled() 
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath));
        }
    }
}
