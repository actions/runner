using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.WebApi
{
    [Serializable]
    public sealed class RunnerNotFoundException : Exception
    {
        public RunnerNotFoundException()
            : base()
        {
        }

        public RunnerNotFoundException(String message)
            : base(message)
        {
        }

        public RunnerNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
