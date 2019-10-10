using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class BuildOrchestrationType
    {
        public const Int32 Build = 1;
        public const Int32 Cleanup = 2;
    }
}
