using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Exceptions
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InvalidQueryStringException : VssServiceException
    {
        public InvalidQueryStringException(string message) : base(message)
        {
        }

        public InvalidQueryStringException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
