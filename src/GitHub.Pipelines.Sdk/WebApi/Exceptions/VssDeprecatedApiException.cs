using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi.Exceptions
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssDeprecatedApiException", "Microsoft.VisualStudio.Services.WebApi.Exceptions.VssDeprecatedApiException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssDeprecatedApiException : VssServiceException
    {
        public VssDeprecatedApiException()
        {
        }

        public VssDeprecatedApiException(string message)
            : base(message)
        {
        }

        public VssDeprecatedApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
