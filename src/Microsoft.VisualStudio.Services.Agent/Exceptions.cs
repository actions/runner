using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class NonRetryableException : Exception
    {
        public NonRetryableException()
            : base()
        { }

        public NonRetryableException(string message)
            : base(message)
        { }

        public NonRetryableException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}