using Microsoft.VisualStudio.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.ClientNotification
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ClientNotificationException", "Microsoft.VisualStudio.Services.ClientNotification.ClientNotificationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ClientNotificationException : VssServiceException
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsRetryable { get; set; }

        public ClientNotificationException()
        {
        }

        public ClientNotificationException(string message)
            : base(message)
        {
        }

        public ClientNotificationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ClientNotificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ClientNotificationServiceNotAvailableException", "Microsoft.VisualStudio.Services.ClientNotification.ClientNotificationServiceNotAvailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ClientNotificationServiceNotAvailableException : ClientNotificationException
    {
        public ClientNotificationServiceNotAvailableException(string message)
            : base(message)
        {
        }

        public ClientNotificationServiceNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "BadClientNotificationSubscriptionRequestException", "Microsoft.VisualStudio.Services.ClientNotification.BadClientNotificationSubscriptionRequestException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadClientNotificationSubscriptionRequestException : ClientNotificationException
    {
        public BadClientNotificationSubscriptionRequestException(string message)
            : base(message)
        {
        }

        public BadClientNotificationSubscriptionRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ClientNotificationSecurityException", "Microsoft.VisualStudio.Services.ClientNotification.ClientNotificationSecurityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ClientNotificationSecurityException : ClientNotificationException
    {
        public ClientNotificationSecurityException(string message)
            : base(message)
        {
        }

        public ClientNotificationSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ServiceIdentityNotSupportedException", "Microsoft.VisualStudio.Services.ClientNotification.ServiceIdentityNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceIdentityNotSupportedException : ClientNotificationException
    {
        public ServiceIdentityNotSupportedException(String message)
            : base(message)
        {
        }

        public ServiceIdentityNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "BadFormatPostNotificationRequestExpcetion", "Microsoft.VisualStudio.Services.ClientNotification.BadFormatPostNotificationRequestExpcetion, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadFormatPostNotificationRequestExpcetion : ClientNotificationException
    {
        public BadFormatPostNotificationRequestExpcetion(string message)
            : base(message)
        {
        }

        public BadFormatPostNotificationRequestExpcetion(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "NotAuthorizedException", "Microsoft.VisualStudio.Services.ClientNotification.NotAuthorizedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NotAuthorizedException : ClientNotificationException
    {
        public NotAuthorizedException(string message)
            : base(message)
        {
        }

        public NotAuthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}