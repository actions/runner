// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcesMoveRequest.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace GitHub.Services.Commerce
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines the request for moving resources across resource groups.
    /// </summary>
    public class ResourcesMoveRequest
    {
        /// <summary>
        /// The target resource group id to move the resources to.
        /// </summary>
        public string TargetResourceGroup
        {
            get; set;
        }

        /// <summary>
        /// The collection of resources to move to the target resource group.
        /// </summary>
        public IEnumerable<string> Resources
        {
            get; set;
        }
    }
}
