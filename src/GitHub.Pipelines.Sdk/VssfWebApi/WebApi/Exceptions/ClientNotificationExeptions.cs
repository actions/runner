using GitHub.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitHub.Services.ClientNotification
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ClientNotificationException", "GitHub.Services.ClientNotification.ClientNotificationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "ClientNotificationServiceNotAvailableException", "GitHub.Services.ClientNotification.ClientNotificationServiceNotAvailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "BadClientNotificationSubscriptionRequestException", "GitHub.Services.ClientNotification.BadClientNotificationSubscriptionRequestException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "ClientNotificationSecurityException", "GitHub.Services.ClientNotification.ClientNotificationSecurityException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "ServiceIdentityNotSupportedException", "GitHub.Services.ClientNotification.ServiceIdentityNotSupportedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "BadFormatPostNotificationRequestExpcetion", "GitHub.Services.ClientNotification.BadFormatPostNotificationRequestExpcetion, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "NotAuthorizedException", "GitHub.Services.ClientNotification.NotAuthorizedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
