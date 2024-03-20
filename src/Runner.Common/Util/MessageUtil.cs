namespace GitHub.Runner.Common.Util
{
    using System;
    using GitHub.DistributedTask.WebApi;

    public static class MessageUtil
    {
        public static bool IsRunServiceJob(string messageType)
        {
            return string.Equals(messageType, JobRequestMessageTypes.RunnerJobRequest, StringComparison.OrdinalIgnoreCase);
        }
    }
}

