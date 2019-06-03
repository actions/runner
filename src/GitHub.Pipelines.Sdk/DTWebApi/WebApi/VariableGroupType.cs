using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class VariableGroupType
    {
        public const String Vsts = "Vsts";
        public const String AzureKeyVault = "AzureKeyVault";
    }
}
