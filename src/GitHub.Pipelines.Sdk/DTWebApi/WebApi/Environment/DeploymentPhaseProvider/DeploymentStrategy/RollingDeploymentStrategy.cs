using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    internal sealed class RollingDeploymentStrategy : DeploymentStrategyBase
    {
        public RollingDeploymentStrategy(RollingDeploymentOption deploymentOption, Int32 deploymentOptionValue, IList<String> selector) : base(DeploymentStrategyType.Rolling)
        {
            DeploymentOption = deploymentOption;
            DeploymentOptionValue = deploymentOptionValue;
            Selector = selector;
        }

        [DataMember]
        public RollingDeploymentOption DeploymentOption
        {
            get;
            private set;
        }

        [DataMember]
        public Int32 DeploymentOptionValue
        {
            get;
            private set;
        }

        [DataMember]
        public IList<String> Selector
        {
            get;
            private set;
        }
    }
}
