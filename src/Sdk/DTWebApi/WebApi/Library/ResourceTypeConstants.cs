using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
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
