using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class JwtResources
    {
        public static string ActorValidationException(params object[] args)
        {
            const string Format = @"The ActorToken within the JsonWebToken is invalid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DeserializationException(params object[] args)
        {
            const string Format = @"Failed to deserialize the JsonWebToken object.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DigestUnsupportedException(params object[] args)
        {
            const string Format = @"JsonWebTokens support only the {0} Digest, but the signing credentials specify {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EncodedTokenDataMalformed(params object[] args)
        {
            const string Format = @"The encoded data in the JsonWebToken is malformed.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidAudienceException(params object[] args)
        {
            const string Format = @"The audience of the token is invalid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidClockSkewException(params object[] args)
        {
            const string Format = @"The value supplied for ClockSkewInSeconds is invalid. It must be a positive integer.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidIssuerException(params object[] args)
        {
            const string Format = @"The issuer of the JsonWebToken is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidSignatureAlgorithm(params object[] args)
        {
            const string Format = @"The signature algorithm in the JsonWebToken header is invalid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidValidFromValueException(params object[] args)
        {
            const string Format = @"The ValidFrom value in not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidValidToValueException(params object[] args)
        {
            const string Format = @"The ValidTo value is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ProviderTypeUnsupported(params object[] args)
        {
            const string Format = @"JsonWebTokens do not support crypto provider of type {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SerializationException(params object[] args)
        {
            const string Format = @"Failed to serialize the JsonWebToken object.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SignatureAlgorithmUnsupportedException(params object[] args)
        {
            const string Format = @"JsonWebTokens do not support the supplied signature algorithm: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SignatureNotFound(params object[] args)
        {
            const string Format = @"The JsonWebToken is not signed, or the signature has not been found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SignatureValidationException(params object[] args)
        {
            const string Format = @"The signature is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SymmetricSecurityKeyNotFound(params object[] args)
        {
            const string Format = @"The supplied Signing Credential is not a SymmetricSigningCredential and does not match the Signature Algorithm.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TokenExpiredException(params object[] args)
        {
            const string Format = @"The token is expired.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TokenNotYetValidException(params object[] args)
        {
            const string Format = @"The token is not yet valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValidFromAfterValidToException(params object[] args)
        {
            const string Format = @"The time represented by the ValidFrom value come after the time represented by the ValidTo value.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SigningTokenExpired(params object[] args)
        {
            const string Format = @"The supplied signing token has expired.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SigningTokenNoPrivateKey(params object[] args)
        {
            const string Format = @"The signing token has no private key and cannot be used for signing.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SigningTokenKeyTooSmall(params object[] args)
        {
            const string Format = @"The key size of the supplied signing token is too small. It must be at least 2048 bits.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TokenScopeNotAuthorizedException(params object[] args)
        {
            const string Format = @"The token scope is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
