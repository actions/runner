using GitHub.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Thrown when service operation is not available
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ServiceOperationNotAvailableException", "GitHub.Services.WebApi.ServiceOperationNotAvailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceOperationNotAvailableException : VssServiceException
    {
        public ServiceOperationNotAvailableException(String message)
            : base(message)
        {
        }

        public ServiceOperationNotAvailableException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
