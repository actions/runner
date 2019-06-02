namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal sealed class RunOnceDeploymentStrategy : DeploymentStrategyBase
    {
        public RunOnceDeploymentStrategy() : base(DeploymentStrategyType.RunOnce)
        {
        }
    }
}