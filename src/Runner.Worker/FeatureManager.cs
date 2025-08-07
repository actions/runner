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

        public static bool IsFeatureEnabled(Variables variables, string featureFlag)
        {
            return variables?.GetBoolean(featureFlag) ?? false;
        }

        public static bool IsUseNode24ByDefaultEnabled(Variables variables)
        {
            return IsFeatureEnabled(variables, Constants.Runner.NodeMigration.UseNode24ByDefaultFlag);
        }

        public static bool IsRequireNode24Enabled(Variables variables)
        {
            return IsFeatureEnabled(variables, Constants.Runner.NodeMigration.RequireNode24Flag);
        }
    }
}
