using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class JwtResources
    {
        public static string ActorValidationException()
        {
            const string Format = @"The ActorToken within the JsonWebToken is invalid.";
            return Format;
        }

        public static string DeserializationException()
        {
            const string Format = @"Failed to deserialize the JsonWebToken object.";
            return Format;
        }

        public static string DigestUnsupportedException(object arg0, object arg1)
        {
            const string Format = @"JsonWebTokens support only the {0} Digest, but the signing credentials specify {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string EncodedTokenDataMalformed()
        {
            const string Format = @"The encoded data in the JsonWebToken is malformed.";
            return Format;
        }

        public static string InvalidAudienceException()
        {
            const string Format = @"The audience of the token is invalid.";
            return Format;
        }

        public static string InvalidClockSkewException()
        {
            const string Format = @"The value supplied for ClockSkewInSeconds is invalid. It must be a positive integer.";
            return Format;
        }

        public static string InvalidIssuerException()
        {
            const string Format = @"The issuer of the JsonWebToken is not valid.";
            return Format;
        }

        public static string InvalidSignatureAlgorithm()
        {
            const string Format = @"The signature algorithm in the JsonWebToken header is invalid.";
            return Format;
        }

        public static string InvalidValidFromValueException()
        {
            const string Format = @"The ValidFrom value in not valid.";
            return Format;
        }

        public static string InvalidValidToValueException()
        {
            const string Format = @"The ValidTo value is not valid.";
            return Format;
        }

        public static string ProviderTypeUnsupported(object arg0)
        {
            const string Format = @"JsonWebTokens do not support crypto provider of type {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SerializationException()
        {
            const string Format = @"Failed to serialize the JsonWebToken object.";
            return Format;
        }

        public static string SignatureAlgorithmUnsupportedException(object arg0)
        {
            const string Format = @"JsonWebTokens do not support the supplied signature algorithm: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SignatureNotFound()
        {
            const string Format = @"The JsonWebToken is not signed, or the signature has not been found.";
            return Format;
        }

        public static string SignatureValidationException()
        {
            const string Format = @"The signature is not valid.";
            return Format;
        }

        public static string TokenExpiredException()
        {
            const string Format = @"The token is expired.";
            return Format;
        }

        public static string TokenNotYetValidException()
        {
            const string Format = @"The token is not yet valid.";
            return Format;
        }

        public static string ValidFromAfterValidToException()
        {
            const string Format = @"The time represented by the ValidFrom value come after the time represented by the ValidTo value.";
            return Format;
        }

        public static string SigningTokenExpired()
        {
            const string Format = @"The supplied signing token has expired.";
            return Format;
        }

        public static string SigningTokenNoPrivateKey()
        {
            const string Format = @"The signing token has no private key and cannot be used for signing.";
            return Format;
        }

        public static string SigningTokenKeyTooSmall()
        {
            const string Format = @"The key size of the supplied signing token is too small. It must be at least 2048 bits.";
            return Format;
        }

        public static string TokenScopeNotAuthorizedException()
        {
            const string Format = @"The token scope is not valid.";
            return Format;
        }
    }
}
