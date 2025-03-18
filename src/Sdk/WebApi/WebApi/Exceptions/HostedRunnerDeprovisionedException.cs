using System;

namespace GitHub.Services.WebApi
{
    [Serializable]
    public sealed class HostedRunnerDeprovisionedException : Exception
    {
        public HostedRunnerDeprovisionedException()
            : base()
        {
        }

        public HostedRunnerDeprovisionedException(String message)
            : base(message)
        {
        }

        public HostedRunnerDeprovisionedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
