using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class ResourceLimitConstants
    {
        public const String FreeCount = "FreeCount";
        public const String PurchasedCount = "PurchasedCount";
        public const String EnterpriseUsersCount = "EnterpriseUsersCount";
        public const String IsPremium = "IsPremium";
    }
}
