using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal abstract class DeploymentStrategyBaseAction
    {
        protected DeploymentStrategyBaseAction(DeploymentStrategyActionType type)
        {
            Type = type;
            Steps = new List<Step>();
        }

        [DataMember]
        public DeploymentStrategyActionType Type { get; set; }

        [DataMember]
        public IList<Step> Steps { get; set; }
    }
}