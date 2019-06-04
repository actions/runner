using System;

namespace GitHub.DistributedTask.WebApi
{
    public static class JobRequestMessageTypes
    {
        public const String AgentJobRequest = "JobRequest";

        public const String ServerJobRequest = "ServerJobRequest";

        public const String ServerTaskRequest = "ServerTaskRequest";

        public const String PipelineAgentJobRequest = "PipelineAgentJobRequest";
    }
}
