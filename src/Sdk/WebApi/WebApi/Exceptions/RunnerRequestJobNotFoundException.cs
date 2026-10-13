using System;

namespace GitHub.Services.WebApi
{
    [Serializable]
    public sealed class RunnerRequestJobNotFoundException : Exception
    {
        public RunnerRequestJobNotFoundException()
            : base()
        {
        }

        public RunnerRequestJobNotFoundException(String message)
            : base(message)
        {
        }

        public RunnerRequestJobNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
