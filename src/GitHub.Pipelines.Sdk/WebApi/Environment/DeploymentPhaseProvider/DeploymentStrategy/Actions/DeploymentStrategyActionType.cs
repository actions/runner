﻿using System;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// DeploymentStrategyActionType.
    /// </summary>
    [Flags]
    internal enum DeploymentStrategyActionType
    {
        /// <summary>
        /// Unknown Action
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Deploy Action
        /// </summary>
        Deploy = 1
    }
}