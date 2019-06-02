using System;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class ResourceTypeConstants
    {
        /// <summary>
        /// Service Endpoints
        /// </summary>
        public const String ServiceEndpoints = "ServiceEndpoints";

        /// <summary>
        /// Variable Groups
        /// </summary>
        public const String VariableGroups = "VariableGroups";

        /// <summary>
        /// Secure Files
        /// </summary>
        public const String SecureFiles = "SecureFiles";
    }
}