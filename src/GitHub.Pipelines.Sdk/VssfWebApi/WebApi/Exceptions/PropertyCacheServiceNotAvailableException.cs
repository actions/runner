using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Thrown when cache service is not available
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "PropertyCacheServiceNotAvailableException", "Microsoft.VisualStudio.Services.WebApi.PropertyCacheServiceNotAvailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class PropertyCacheServiceNotAvailableException : VssServiceException
    {
        public PropertyCacheServiceNotAvailableException(String message)
            : base(message)
        {
        }

        public PropertyCacheServiceNotAvailableException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
