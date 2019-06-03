using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.TokenAdmin.Client
{
    [Serializable]
    public class TokenAdminException : VssServiceException
    {
        public TokenAdminException()
        { }

        public TokenAdminException(string message)
            : base(message)
        { }

        public TokenAdminException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TokenAdminException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }


    public class TokenAdminUnauthorizedException : TokenAdminException
    {
        public TokenAdminUnauthorizedException()
        { }

        public TokenAdminUnauthorizedException(string message)
            : base(message)
        { }

        public TokenAdminUnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TokenAdminUnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    public class TokenAdminInvalidPageSizeException : TokenAdminException
    {
        public TokenAdminInvalidPageSizeException()
        { }

        public TokenAdminInvalidPageSizeException(string message)
            : base(message)
        { }

        public TokenAdminInvalidPageSizeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TokenAdminInvalidPageSizeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
    
    public class TokenAdminBadRequestException : TokenAdminException
    {
        public TokenAdminBadRequestException()
        { }

        public TokenAdminBadRequestException(string message)
            : base(message)
        { }

        public TokenAdminBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TokenAdminBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
