﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using GitHub.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Services.WebApi.Jwt
{
    public static class JsonWebTokenUtilities
    {
        static JsonWebTokenUtilities()
        {
            DefaultSerializerSettings = new JsonSerializerSettings();
            DefaultSerializerSettings.Converters.Add(new UnixEpochDateTimeConverter());
            DefaultSerializerSettings.Converters.Add(new StringEnumConverter());
        }

        internal static readonly JsonSerializerSettings DefaultSerializerSettings;

        internal static readonly short MinKeySize = 2048;

        internal static string JsonEncode<T>(this T obj)
        {
            return JsonEncode((object)obj);
        }

        internal static string JsonEncode(object o)
        {
            ArgumentUtility.CheckForNull(o, nameof(o));

            string json = JsonConvert.SerializeObject(o, DefaultSerializerSettings);

            return Encoding.UTF8.GetBytes(json).ToBase64StringNoPadding();
        }

        internal static T JsonDecode<T>(string encodedString)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(encodedString, nameof(encodedString));

            byte[] bytes = encodedString.FromBase64StringNoPadding();

            string json = Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<T>(json, DefaultSerializerSettings);
        }

        internal static IDictionary<string, object> TranslateToJwtClaims(IEnumerable<Claim> claims)
        {
            ArgumentUtility.CheckForNull(claims, nameof(claims));

            Dictionary<string, object> ret = new Dictionary<string, object>();

            //there are two claim names that get special treatment
            foreach (Claim claim in claims)
            {
                string claimName = claim.Type;
                if (string.Compare(claimName, JsonWebTokenClaims.NameIdLongName, StringComparison.Ordinal) == 0)
                {
                    claimName = JsonWebTokenClaims.NameId;
                }
                else if (string.Compare(claimName, JsonWebTokenClaims.IdentityProviderLongName, StringComparison.Ordinal) == 0)
                {
                    claimName = JsonWebTokenClaims.IdentityProvider;
                }

                ret.Add(claimName, claim.Value);
            }

            return ret;
        }

        internal static IEnumerable<Claim> TranslateFromJwtClaims(IDictionary<string, object> claims)
        {
            ArgumentUtility.CheckForNull(claims, nameof(claims));

            List<Claim> ret = new List<Claim>();

            //there are two claim names that get special treatment
            foreach (var claim in claims)
            {
                string claimName = claim.Key;
                if (string.Compare(claimName, JsonWebTokenClaims.NameId, StringComparison.Ordinal) == 0)
                {
                    claimName = JsonWebTokenClaims.NameIdLongName;
                }
                else if (string.Compare(claimName, JsonWebTokenClaims.IdentityProvider, StringComparison.Ordinal) == 0)
                {
                    claimName = JsonWebTokenClaims.IdentityProviderLongName;
                }

                ret.Add(new Claim(claimName, claim.Value.ToString()));
            }

            return ret;
        }

        public static IEnumerable<Claim> ExtractClaims(this JsonWebToken token)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));

            return TranslateFromJwtClaims(token.Payload);
        }

        public static bool IsExpired(this JsonWebToken token)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));

            return DateTime.UtcNow > token.ValidTo;
        }

        internal static JWTAlgorithm ValidateSigningCredentials(VssSigningCredentials credentials, bool allowExpiredToken = false)
        {
            if (credentials == null)
            {
                return JWTAlgorithm.None;
            }

            if (!credentials.CanSignData)
            {
                throw new InvalidCredentialsException(JwtResources.SigningTokenNoPrivateKey());
            }

            if (!allowExpiredToken && credentials.ValidTo.ToUniversalTime() < (DateTime.UtcNow - TimeSpan.FromMinutes(5)))
            {
                throw new InvalidCredentialsException(JwtResources.SigningTokenExpired());
            }

            return credentials.SignatureAlgorithm;
        }

        public static ClaimsPrincipal ValidateToken(this JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            ClaimsIdentity actorIdentity = ValidateActor(token, parameters);
            ValidateLifetime(token, parameters);
            ValidateAudience(token, parameters);
            ValidateSignature(token, parameters);
            ValidateIssuer(token, parameters);

            ClaimsIdentity identity = new ClaimsIdentity("Federation", parameters.IdentityNameClaimType, ClaimTypes.Role);

            if (actorIdentity != null)
            {
                identity.Actor = actorIdentity;
            }

            IEnumerable<Claim> claims = token.ExtractClaims();

            foreach (Claim claim in claims)
            {
                identity.AddClaim(new Claim(claim.Type, claim.Value, claim.ValueType, token.Issuer));
            }

            return new ClaimsPrincipal(identity);
        }

        private static ClaimsIdentity ValidateActor(JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            if (!parameters.ValidateActor)
            {
                return null;
            }

            //this recursive call with check the parameters
            ClaimsPrincipal principal = token.Actor.ValidateToken(parameters.ActorValidationParameters);

            if (!(principal?.Identity is ClaimsIdentity))
            {
                throw new ActorValidationException();
            }

            return (ClaimsIdentity)principal.Identity;
        }

        private static void ValidateLifetime(JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            if ((parameters.ValidateNotBefore || parameters.ValidateExpiration) && (parameters.ClockSkewInSeconds < 0))
            {
                throw new InvalidClockSkewException();
            }

            TimeSpan skew = TimeSpan.FromSeconds(parameters.ClockSkewInSeconds);

            if (parameters.ValidateNotBefore && token.ValidFrom == default(DateTime))
            {
                throw new InvalidValidFromValueException();
            }

            if (parameters.ValidateExpiration && token.ValidTo == default(DateTime))
            {
                throw new InvalidValidToValueException();
            }

            if (parameters.ValidateExpiration && parameters.ValidateNotBefore && (token.ValidFrom > token.ValidTo))
            {
                throw new ValidFromAfterValidToException();
            }

            if (parameters.ValidateNotBefore && (token.ValidFrom > (DateTime.UtcNow + skew)))
            {
                throw new TokenNotYetValidException(); //validation exception
            }

            if (parameters.ValidateExpiration && (token.ValidTo < (DateTime.UtcNow - skew)))
            {
                throw new TokenExpiredException(); //validation exception
            }
        }

        private static void ValidateAudience(JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            if (!parameters.ValidateAudience)
            {
                return;
            }

            ArgumentUtility.CheckStringForNullOrEmpty(token.Audience, nameof(token.Audience));
            ArgumentUtility.CheckEnumerableForNullOrEmpty(parameters.AllowedAudiences, nameof(parameters.AllowedAudiences));

            foreach (string audience in parameters.AllowedAudiences)
            {
                if (string.Compare(audience, token.Audience, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return;
                }
            }

            throw new InvalidAudienceException(); //validation exception;            
        }

        private static void ValidateSignature(JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            if (!parameters.ValidateSignature)
            {
                return;
            }

            string encodedData = token.EncodedToken;

            string[] parts = encodedData.Split('.');

            if (parts.Length != 3)
            {
                throw new InvalidTokenException(JwtResources.EncodedTokenDataMalformed()); //validation exception
            }

            if (string.IsNullOrEmpty(parts[2]))
            {
                throw new InvalidTokenException(JwtResources.SignatureNotFound()); //validation exception
            }

            if (token.Algorithm == JWTAlgorithm.None)
            {
                throw new InvalidTokenException(JwtResources.InvalidSignatureAlgorithm()); //validation exception
            }

            ArgumentUtility.CheckForNull(parameters.SigningCredentials, nameof(parameters.SigningCredentials));

            //ArgumentUtility.CheckEnumerableForNullOrEmpty(parameters.SigningToken.SecurityKeys, nameof(parameters.SigningToken.SecurityKeys));

            byte[] sourceInput = Encoding.UTF8.GetBytes(string.Format("{0}.{1}", parts[0], parts[1]));

            byte[] sourceSignature = parts[2].FromBase64StringNoPadding();


            try
            {
                if (parameters.SigningCredentials.VerifySignature(sourceInput, sourceSignature))
                {
                    return;
                }
            }
            catch (Exception)
            {
                //swallow exceptions here, we'll throw if nothing works...
            }

            throw new SignatureValidationException(); //valiation exception
        }

        private static void ValidateIssuer(JsonWebToken token, JsonWebTokenValidationParameters parameters)
        {
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            if (!parameters.ValidateIssuer)
            {
                return;
            }

            ArgumentUtility.CheckStringForNullOrEmpty(token.Issuer, nameof(token.Issuer));
            ArgumentUtility.CheckEnumerableForNullOrEmpty(parameters.ValidIssuers, nameof(parameters.ValidIssuers));

            foreach (string issuer in parameters.ValidIssuers)
            {
                if (string.Compare(issuer, token.Issuer, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return;
                }
            }

            throw new InvalidIssuerException(); //validation exception;
        }
    }
}
