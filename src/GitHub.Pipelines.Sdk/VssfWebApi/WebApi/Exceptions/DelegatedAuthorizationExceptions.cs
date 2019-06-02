using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "FailedToIssueAccessTokenException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.FailedToIssueAccessTokenException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class FailedToIssueAccessTokenException : VssServiceException
    {
        public FailedToIssueAccessTokenException(string message) : base(message)
        {
        }

        public FailedToIssueAccessTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SessionTokenCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.SessionTokenCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SessionTokenCreateException : VssServiceException
    {
        public SessionTokenCreateException(string message) : base(message)
        {
        }

        public SessionTokenCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RegistrationNotFoundException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.RegistrationNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RegistrationNotFoundException : VssServiceException
    {

        public RegistrationNotFoundException()
            : base()
        {
        }

        public RegistrationNotFoundException(string message) : base(message)
        {
        }


        public RegistrationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RegistrationAlreadyExistsException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.RegistrationAlreadyExistsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RegistrationAlreadyExistsException : VssServiceException
    {

        public RegistrationAlreadyExistsException()
            : base()
        {
        }

        public RegistrationAlreadyExistsException(string message) : base(message)
        {
        }


        public RegistrationAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SessionTokenNotFoundException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.SessionTokenNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SessionTokenNotFoundException : VssServiceException
    {
        public SessionTokenNotFoundException(string message) : base(message)
        {
        }

        public SessionTokenNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RegistrationCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.RegistrationCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RegistrationCreateException : VssServiceException
    {
        public RegistrationCreateException(string message) : base(message)
        {
        }

        public RegistrationCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "HostAuthorizationCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.HostAuthorizationCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class HostAuthorizationCreateException : VssServiceException
    {
        public HostAuthorizationCreateException(string message) : base(message)
        {
        }

        public HostAuthorizationCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RegistrationUpdateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.RegistrationUpdateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RegistrationUpdateException : VssServiceException
    {
        public RegistrationUpdateException(string message) : base(message)
        {
        }

        public RegistrationUpdateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TokenPairCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.TokenPairCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TokenPairCreateException : VssServiceException
    {
        public TokenPairCreateException(string message) : base(message)
        {
        }

        public TokenPairCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AppSessionTokenCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.AppSessionTokenCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AppSessionTokenCreateException : VssServiceException
    {
        public AppSessionTokenCreateException(string message) : base(message)
        {
        }

        public AppSessionTokenCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class PlatformDelegatedAuthorizationException : VssServiceException
    {
        public PlatformDelegatedAuthorizationException(string message) : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ExchangeAppTokenCreateException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.ExchangeAppTokenCreateException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ExchangeAppTokenCreateException : VssServiceException
    {
        public ExchangeAppTokenCreateException(string message) : base(message)
        {
        }

        public ExchangeAppTokenCreateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ExchangeAppTokenNotFoundException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.ExchangeAppTokenNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ExchangeAppTokenNotFoundException : VssServiceException
    {
        public ExchangeAppTokenNotFoundException(string message) : base(message)
        {
        }

        public ExchangeAppTokenNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AuthorizationIdNotFoundException", "Microsoft.VisualStudio.Services.DelegatedAuthorization.AuthorizationIdNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AuthorizationIdNotFoundException : VssServiceException
    {
        public AuthorizationIdNotFoundException(string message) : base(message)
        {

        }

        public AuthorizationIdNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class ExchangeAccessTokenKeyException : VssServiceException
    {
        public ExchangeAccessTokenKeyException(string message) : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InvalidPublicKeyException : VssServiceException
    {
        public InvalidPublicKeyException(string message) : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InvalidPersonalAccessTokenException : VssServiceException
    {
        public InvalidPersonalAccessTokenException(string message) : base(message)
        {
        }
    }
}
