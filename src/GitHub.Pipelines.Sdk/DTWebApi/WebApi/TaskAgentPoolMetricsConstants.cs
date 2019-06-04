using System;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [GenerateAllConstants]
    public class TaskAgentPoolMetricsValidAgentState
    {
        public const String Online = "Online";
        public const String Offline = "Offline";
    }

    [GenerateAllConstants]
    public class TaskAgentPoolMetricsValidColumnNames
    {
        public const String AgentState = "AgentState";
        public const String AgentsCount = "AgentsCount";
    }

    [GenerateAllConstants]
    public class TaskAgentPoolMetricsValidColumnValueTypes
    {
        public const String Number = "number";
        public const String String = "string";
    }
}
