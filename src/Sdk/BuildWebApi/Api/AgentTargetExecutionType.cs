using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi {

    [GenerateAllConstants]
    public static class AgentTargetExecutionType {
        public const Int32 Normal = 0;
        public const Int32 VariableMultipliers = 1;
        public const Int32 MultipleAgents = 2;
    }
}
