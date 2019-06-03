using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Identity.Mru
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMruException", "GitHub.Services.Identity.Mru.IdentityMruException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMruException : VssServiceException
    {
        public IdentityMruException()
        { }

        public IdentityMruException(string message)
            : base(message)
        { }

        public IdentityMruException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityMruException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMruBadRequestException", "GitHub.Services.Identity.Mru.IdentityMruBadRequestException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMruBadRequestException : IdentityMruException
    {
        protected IdentityMruBadRequestException()
        { }

        public IdentityMruBadRequestException(string message)
            : base(message)
        { }

        public IdentityMruBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityMruBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMruResourceExistsException", "GitHub.Services.Identity.Mru.IdentityMruResourceExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMruResourceExistsException : IdentityMruException
    {
        public IdentityMruResourceExistsException()
        { }

        public IdentityMruResourceExistsException(string message)
            : base(message)
        { }

        public IdentityMruResourceExistsException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityMruResourceExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMruResourceNotFoundException", "GitHub.Services.Identity.Mru.IdentityMruResourceNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMruResourceNotFoundException : IdentityMruException
    {
        protected IdentityMruResourceNotFoundException()
        { }

        public IdentityMruResourceNotFoundException(string message)
            : base(message)
        { }

        public IdentityMruResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityMruResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMruUnauthorizedException", "GitHub.Services.Identity.Mru.IdentityMruUnauthorizedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMruUnauthorizedException : IdentityMruException
    {
        protected IdentityMruUnauthorizedException()
        { }

        public IdentityMruUnauthorizedException(string message)
            : base(message)
        { }

        public IdentityMruUnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityMruUnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

}
