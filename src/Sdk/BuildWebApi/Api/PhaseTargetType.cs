using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class PhaseTargetType
    {
        public const Int32 Agent = 1;
        public const Int32 Server = 2;
    }
}
