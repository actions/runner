using System.Collections.Generic;

namespace GitHub.DistributedTask.WebApi
{
    internal abstract class DeploymentStrategyBase
    {
        protected DeploymentStrategyBase(DeploymentStrategyType type)
        {
            Type = type;
            Actions = new List<DeploymentStrategyBaseAction>();
        }

        public DeploymentStrategyType Type { get; set; }

        public IList<DeploymentStrategyBaseAction> Actions { get; set; }
    }
}
