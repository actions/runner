using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;

namespace GitHub.Services.OAuthWhitelist
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "OAuthWhitelistEntryAlreadyExistsException", "GitHub.Services.OAuthWhitelist.OAuthWhitelistEntryAlreadyExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class OAuthWhitelistEntryAlreadyExistsException : VssServiceException
    {

        public OAuthWhitelistEntryAlreadyExistsException()
            : base()
        {
        }

        public OAuthWhitelistEntryAlreadyExistsException(string message) : base(message)
        {
        }


        public OAuthWhitelistEntryAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "OAuthWhitelistEntryNotFoundException", "GitHub.Services.OAuthWhitelist.OAuthWhitelistEntryNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class OAuthWhitelistEntryNotFoundException : VssServiceException
    {

        public OAuthWhitelistEntryNotFoundException()
            : base()
        {
        }

        public OAuthWhitelistEntryNotFoundException(string message) : base(message)
        {
        }


        public OAuthWhitelistEntryNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "OAuthWhitelistUpdateNotSupportedException", "GitHub.Services.OAuthWhitelist.OAuthWhitelistUpdateNotSupportedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class OAuthWhitelistUpdateNotSupportedException : VssServiceException
    {

        public OAuthWhitelistUpdateNotSupportedException()
            : base()
        {
        }

        public OAuthWhitelistUpdateNotSupportedException(string message) : base(message)
        {
        }


        public OAuthWhitelistUpdateNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
