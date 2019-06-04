using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Jwt;
using Newtonsoft.Json;

namespace GitHub.Services.Licensing
{
    public static class LicensingUtility
    {
        public static IClientRightsEnvelope ParseClientRightsToken(string clientRightsToken, X509Certificate2 certificate)
        {
            // Validation
            ArgumentUtility.CheckForNull(clientRightsToken, "clientRightsToken");
            ArgumentUtility.CheckForNull(certificate, "certificate");

            var jwt = JsonWebToken.Create(clientRightsToken);
            var validationParameters = new JsonWebTokenValidationParameters
            {
                SigningCredentials = VssSigningCredentials.Create(certificate),
                ValidateActor = false,
                ValidateAudience = false,
                ValidateExpiration = false,
                ValidateIssuer = false,
                ValidateNotBefore = false,
                ValidateSignature = true,
            };

            return ParseJwt(jwt, validationParameters);
        }

        internal static IClientRightsEnvelope ParseJwt(JsonWebToken jwt, JsonWebTokenValidationParameters validationParameters)
        {
            var claimsPrincipal = jwt.ValidateToken(validationParameters);

            var claims = claimsPrincipal.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);

            var rights = JsonConvert.DeserializeObject<ClientRight[]>(GetRequiredClientRightsAttribute(claims, ClientRightsEnvelopeClaims.Rights), new VersionConverter());

            var envelope = new ClientRightsEnvelope(rights);
            envelope.EnvelopeVersion = new Version(GetRequiredClientRightsAttribute(claims, ClientRightsEnvelopeClaims.EnvelopeVersion));
            envelope.UserId = new Guid(GetRequiredClientRightsAttribute(claims, ClientRightsEnvelopeClaims.UserId));
            envelope.UserName = GetOptionalClientRightsAttribute(claims, ClientRightsEnvelopeClaims.UserName, string.Empty);
            envelope.RefreshInterval = TimeSpan.FromSeconds(int.Parse(GetRequiredClientRightsAttribute(claims, ClientRightsEnvelopeClaims.RefreshInterval)));
            envelope.ActivityId = new Guid(GetRequiredClientRightsAttribute(claims, ClientRightsEnvelopeClaims.ActivityId));
            envelope.CreationDate = jwt.ValidFrom.ToUniversalTime();
            envelope.ExpirationDate = jwt.ValidTo.ToUniversalTime();
            envelope.Canary = GetOptionalClientRightsAttribute(claims, ClientRightsEnvelopeClaims.Canary, string.Empty);

            return envelope;
        }

        #region Private helpers

        private static string GetRequiredClientRightsAttribute(Dictionary<string, string> claims, string name)
        {
            return claims[name];
        }

        private static string GetOptionalClientRightsAttribute(Dictionary<string, string> claims, string name, string defaultValue)
        {
            string s = null;
            return claims.TryGetValue(name, out s) ? s : defaultValue;
        }

        private class VersionConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // default serialization
                serializer.Serialize(writer, value);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var dict = serializer.Deserialize<Dictionary<string, int>>(reader);
                return new Version(dict["Major"], dict["Minor"], dict["Build"], dict["Revision"]);
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Version);
            }
        }

        #endregion
    }
}
