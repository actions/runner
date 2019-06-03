using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.UserMapping
{
    [Serializable]
    public class UserMappingException : VssServiceException
    {
        public UserMappingException()
        { }

        public UserMappingException(string message)
            : base(message)
        { }

        public UserMappingException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected UserMappingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    #region Common Exceptions

    [Serializable]
    public class UserMappingBadRequestException : UserMappingException
    {
        protected UserMappingBadRequestException()
        {
        }

        public UserMappingBadRequestException(string message)
            : base(message)
        {
        }

        public UserMappingBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserMappingBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class UserAccountMappingNotFoundException : UserMappingException
    {
        public UserAccountMappingNotFoundException()
        {
        }

        public UserAccountMappingNotFoundException(string message)
            : base(message)
        {
        }

        public UserAccountMappingNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserAccountMappingNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Authorization related

    [Serializable]
    public class UserMappingServiceSecurityException : UserMappingException
    {
        public UserMappingServiceSecurityException()
        {
        }

        public UserMappingServiceSecurityException(string message)
            : base(message)
        {
        }

        public UserMappingServiceSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserMappingServiceSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion
}
