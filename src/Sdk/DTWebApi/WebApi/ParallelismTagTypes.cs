using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public static class ParallelismTagTypes
    {
        public const String Public = "Public";
        public const String Private = "Private";
    }
}
