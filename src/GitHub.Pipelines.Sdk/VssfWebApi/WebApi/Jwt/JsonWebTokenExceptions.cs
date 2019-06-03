using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Jwt
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenException", "GitHub.Services.WebApi.Jwt.JsonWebTokenException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenValidationException", "GitHub.Services.WebApi.Jwt.JsonWebTokenValidationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenSerializationException", "GitHub.Services.WebApi.Jwt.JsonWebTokenSerializationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenSerializationException : JsonWebTokenException
    {
        public JsonWebTokenSerializationException() : base(JwtResources.SerializationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "JsonWebTokenDeserializationException", "GitHub.Services.WebApi.Jwt.JsonWebTokenDeserializationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class JsonWebTokenDeserializationException : JsonWebTokenException
    {
        public JsonWebTokenDeserializationException()
            : base(JwtResources.DeserializationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "DigestUnsupportedException", "GitHub.Services.WebApi.Jwt.DigestUnsupportedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DigestUnsupportedException : JsonWebTokenException
    {
        public DigestUnsupportedException(string supportedDigest, string invalidDigest)
            : base(JwtResources.DigestUnsupportedException(supportedDigest, invalidDigest))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidCredentialsException", "GitHub.Services.WebApi.Jwt.InvalidCredentialsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidCredentialsException : JsonWebTokenException
    {
        public InvalidCredentialsException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SignatureAlgorithmUnsupportedException", "GitHub.Services.WebApi.Jwt.SignatureAlgorithmUnsupportedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "InvalidClockSkewException", "GitHub.Services.WebApi.Jwt.InvalidClockSkewException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidClockSkewException : JsonWebTokenException
    {
        public InvalidClockSkewException()
            : base(JwtResources.InvalidClockSkewException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidValidFromValueException", "GitHub.Services.WebApi.Jwt.InvalidValidFromValueException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidValidFromValueException : JsonWebTokenException
    {
        public InvalidValidFromValueException()
            : base(JwtResources.InvalidValidFromValueException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidValidToValueException", "GitHub.Services.WebApi.Jwt.InvalidValidToValueException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidValidToValueException : JsonWebTokenException
    {
        public InvalidValidToValueException()
            : base(JwtResources.InvalidValidToValueException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ValidFromAfterValidToException", "GitHub.Services.WebApi.Jwt.ValidFromAfterValidToException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ValidFromAfterValidToException : JsonWebTokenException
    {
        public ValidFromAfterValidToException()
            : base(JwtResources.ValidFromAfterValidToException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ActorValidationException", "GitHub.Services.WebApi.Jwt.ActorValidationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ActorValidationException : JsonWebTokenValidationException
    {
        public ActorValidationException()
            : base(JwtResources.ActorValidationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TokenNotYetValidException", "GitHub.Services.WebApi.Jwt.TokenNotYetValidException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TokenNotYetValidException : JsonWebTokenValidationException
    {
        public TokenNotYetValidException()
            : base(JwtResources.TokenNotYetValidException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TokenExpiredException", "GitHub.Services.WebApi.Jwt.TokenExpiredException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TokenExpiredException : JsonWebTokenValidationException
    {
        public TokenExpiredException()
            : base(JwtResources.TokenExpiredException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidAudienceException", "GitHub.Services.WebApi.Jwt.InvalidAudienceException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidAudienceException : JsonWebTokenValidationException
    {
        public InvalidAudienceException()
            : base(JwtResources.InvalidAudienceException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidTokenException", "GitHub.Services.WebApi.Jwt.InvalidTokenException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidTokenException : JsonWebTokenValidationException
    {
        public InvalidTokenException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SignatureValidationException", "GitHub.Services.WebApi.Jwt.SignatureValidationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SignatureValidationException : JsonWebTokenValidationException
    {
        public SignatureValidationException()
            : base(JwtResources.SignatureValidationException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIssuerException", "GitHub.Services.WebApi.Jwt.InvalidIssuerException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIssuerException : JsonWebTokenValidationException
    {
        public InvalidIssuerException()
            : base(JwtResources.InvalidIssuerException())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidScopeException", "GitHub.Services.WebApi.Jwt.InvalidScopeException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidScopeException : JsonWebTokenValidationException
    {
        public InvalidScopeException()
            : base(JwtResources.TokenScopeNotAuthorizedException())
        {

        }
    }
}
