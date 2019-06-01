using System;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class AzurePermissionResourceProviders
    {
        public const String AzureRoleAssignmentPermission = "Microsoft.RoleAssignment";
        public const String AzureKeyVaultPermission = "Microsoft.KeyVault";
    }
}
