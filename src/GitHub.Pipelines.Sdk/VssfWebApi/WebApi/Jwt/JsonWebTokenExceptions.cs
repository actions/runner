using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi.Jwt
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenException", "Microsoft.VisualStudio.Services.WebApi.Jwt.JsonWebTokenException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenException : VssServiceException
    {
        public JsonWebTokenException(string message)
            : base(message)
        {
        }

        public JsonWebTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenValidationException", "Microsoft.VisualStudio.Services.WebApi.Jwt.JsonWebTokenValidationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenValidationException : JsonWebTokenException
    {
        public JsonWebTokenValidationException(string message)
            : base(message)
        {
        }

        public JsonWebTokenValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenSerializationException", "Microsoft.VisualStudio.Services.WebApi.Jwt.JsonWebTokenSerializationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenSerializationException : JsonWebTokenException
    {
        public JsonWebTokenSerializationException() : base(JwtResources.SerializationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenDeserializationException", "Microsoft.VisualStudio.Services.WebApi.Jwt.JsonWebTokenDeserializationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenDeserializationException : JsonWebTokenException
    {
        public JsonWebTokenDeserializationException()
            : base(JwtResources.DeserializationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "DigestUnsupportedException", "Microsoft.VisualStudio.Services.WebApi.Jwt.DigestUnsupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DigestUnsupportedException : JsonWebTokenException
    {
        public DigestUnsupportedException(string supportedDigest, string invalidDigest)
            : base(JwtResources.DigestUnsupportedException(supportedDigest, invalidDigest))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidCredentialsException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidCredentialsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidCredentialsException : JsonWebTokenException
    {
        public InvalidCredentialsException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SignatureAlgorithmUnsupportedException", "Microsoft.VisualStudio.Services.WebApi.Jwt.SignatureAlgorithmUnsupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SignatureAlgorithmUnsupportedException : JsonWebTokenException
    {
        public SignatureAlgorithmUnsupportedException(string invalidAlgorithm)
            : base(JwtResources.SignatureAlgorithmUnsupportedException(invalidAlgorithm))
        {
        }

        public SignatureAlgorithmUnsupportedException(int providerType)
            : base(JwtResources.ProviderTypeUnsupported(providerType))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidClockSkewException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidClockSkewException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidClockSkewException : JsonWebTokenException
    {
        public InvalidClockSkewException()
            : base(JwtResources.InvalidClockSkewException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidValidFromValueException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidValidFromValueException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidValidFromValueException : JsonWebTokenException
    {
        public InvalidValidFromValueException()
            : base(JwtResources.InvalidValidFromValueException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidValidToValueException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidValidToValueException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidValidToValueException : JsonWebTokenException
    {
        public InvalidValidToValueException()
            : base(JwtResources.InvalidValidToValueException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ValidFromAfterValidToException", "Microsoft.VisualStudio.Services.WebApi.Jwt.ValidFromAfterValidToException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ValidFromAfterValidToException : JsonWebTokenException
    {
        public ValidFromAfterValidToException()
            : base(JwtResources.ValidFromAfterValidToException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ActorValidationException", "Microsoft.VisualStudio.Services.WebApi.Jwt.ActorValidationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ActorValidationException : JsonWebTokenValidationException
    {
        public ActorValidationException()
            : base(JwtResources.ActorValidationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TokenNotYetValidException", "Microsoft.VisualStudio.Services.WebApi.Jwt.TokenNotYetValidException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TokenNotYetValidException : JsonWebTokenValidationException
    {
        public TokenNotYetValidException()
            : base(JwtResources.TokenNotYetValidException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TokenExpiredException", "Microsoft.VisualStudio.Services.WebApi.Jwt.TokenExpiredException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TokenExpiredException : JsonWebTokenValidationException
    {
        public TokenExpiredException()
            : base(JwtResources.TokenExpiredException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidAudienceException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidAudienceException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidAudienceException : JsonWebTokenValidationException
    {
        public InvalidAudienceException()
            : base(JwtResources.InvalidAudienceException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidTokenException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidTokenException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidTokenException : JsonWebTokenValidationException
    {
        public InvalidTokenException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SignatureValidationException", "Microsoft.VisualStudio.Services.WebApi.Jwt.SignatureValidationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SignatureValidationException : JsonWebTokenValidationException
    {
        public SignatureValidationException()
            : base(JwtResources.SignatureValidationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIssuerException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidIssuerException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIssuerException : JsonWebTokenValidationException
    {
        public InvalidIssuerException()
            : base(JwtResources.InvalidIssuerException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidScopeException", "Microsoft.VisualStudio.Services.WebApi.Jwt.InvalidScopeException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidScopeException : JsonWebTokenValidationException
    {
        public InvalidScopeException()
            : base(JwtResources.TokenScopeNotAuthorizedException())
        {

        }
    }
}
