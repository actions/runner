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
    
        public static void EnsureContainerOperationsFeature(Variables variables)
        {
            if (Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                return;
            }
            else if(Constants.Runner.Platform.Equals(Constants.OSPlatform.Windows))
            {
                var isContainerOnWindowsFeatureFlagSet = bool.TryParse(Environment.GetEnvironmentVariable(Constants.Runner.Features.AllowContainerOperationsOnWindows) ?? "false", out var b) && b;
                if (!isContainerOnWindowsFeatureFlagSet)
                {
                    throw new NotSupportedException($"Container operations are not supported on Windows runners (experimental support can be enabled via '{Constants.Runner.Features.AllowContainerOperationsOnWindows}')");
                }
            }
            else
            {
                throw new NotSupportedException($"Container operations are not supported on macOS runners");
            }
        }
    }
}
