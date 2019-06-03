using GitHub.Services.Common.Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitHub.Services.Common
{
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssAuthenticationException", "GitHub.Services.Common.VssAuthenticationException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssAuthenticationException : VssException
    {
        public VssAuthenticationException()
        {
        }

        public VssAuthenticationException(String message)
            : base(message)
        {
        }

        public VssAuthenticationException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected VssAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssUnauthorizedException", "GitHub.Services.Common.VssUnauthorizedException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssUnauthorizedException : VssException
    {
        public VssUnauthorizedException()
            : this(CommonResources.VssUnauthorizedUnknownServer())
        {
        }

        public VssUnauthorizedException(String message)
            : base(message)
        {
        }

        public VssUnauthorizedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected VssUnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
