using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi {

    [GenerateAllConstants]
    public static class ServerTargetExecutionType {
        public const Int32 Normal = 0;
        public const Int32 VariableMultipliers = 1;
    }
}
