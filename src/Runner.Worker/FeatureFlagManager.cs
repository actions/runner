using System;
using System.IO;
using System.Linq;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker
{
    public class FeatureFlagManager
    {
        public static bool IsContainerHooksEnabled(Variables variables)
        {
            var isContainerHookFeatureFlagSet = variables?.GetBoolean(Constants.Runner.Features.AllowRunnerContainerHooks) ?? true;
            var isContainerHooksPathSet = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath));
            return isContainerHookFeatureFlagSet && isContainerHooksPathSet;
        }
    }
}
