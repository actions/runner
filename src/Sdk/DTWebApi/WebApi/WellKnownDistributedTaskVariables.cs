using System;

namespace GitHub.DistributedTask.WebApi
{
    public static class WellKnownDistributedTaskVariables
    {
        public static readonly String JobId = "system.jobId";
        public static readonly String RunnerLowDiskspaceThreshold = "system.runner.lowdiskspacethreshold";
        public static readonly String RunnerEnvironment = "system.runnerEnvironment";
    }
}
