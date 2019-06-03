using System;

namespace Runner.Common
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
