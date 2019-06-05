using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class AzurePermissionResourceProviders
    {
        public const String AzureRoleAssignmentPermission = "Microsoft.RoleAssignment";
        public const String AzureKeyVaultPermission = "Microsoft.KeyVault";
    }
}
