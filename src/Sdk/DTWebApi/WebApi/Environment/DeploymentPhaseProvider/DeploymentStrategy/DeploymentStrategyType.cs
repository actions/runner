using System;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// DeploymentStrategyType.
    /// </summary>
    [Flags]
    internal enum DeploymentStrategyType
    {
        /// <summary>
        /// Unknown Deployment Strategy
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// RunOnce Deployment Strategy
        /// </summary>
        RunOnce = 1,

        /// <summary>
        /// Rolling Deployment Strategy
        /// </summary>
        Rolling = 2
    }
}
