using System;

namespace GitHub.DistributedTask.WebApi
{
    internal static class DeploymentStrategyConstants
    {
        internal const String DeploymentStrategyRunOnce = "runOnce";
        internal const String DeploymentStrategyRolling = "rolling";
        internal const String RollingDeploymentMaxBatchSize = "maxBatchSize";
        internal const String RollingDeploymentSelector = "selector";
        internal const String StepsPropertyName = "steps";
        internal const String StrategyDeployActionName = "deploy";
    }
}
