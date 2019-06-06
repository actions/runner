using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Jwt;
using Newtonsoft.Json.Linq;
using System;

namespace GitHub.Services.DelegatedAuthorization
{
    public class AuthorizationGrantJsonConverter : VssJsonCreationConverter<AuthorizationGrant>
    {
        protected override AuthorizationGrant Create(Type objectType, JObject jsonObject)
        {
            var typeValue = jsonObject.GetValue(nameof(AuthorizationGrant.GrantType), StringComparison.OrdinalIgnoreCase);
            if (typeValue == null)
            {
                throw new ArgumentException(WebApiResources.UnknownEntityType(typeValue));
            }

            GrantType grantType;
            if (typeValue.Type == JTokenType.Integer)
            {
                grantType = (GrantType)(Int32)typeValue;
            }
            else if (typeValue.Type != JTokenType.String || !Enum.TryParse((String)typeValue, out grantType))
            {
                return null;
            }

            AuthorizationGrant authorizationGrant = null;
            var jwtObject = jsonObject.GetValue("jwt");
            if (jwtObject == null)
            {
                return null;
            }

            JsonWebToken jwt = JsonWebToken.Create(jwtObject.ToString());
            switch (grantType)
            {
                case GrantType.JwtBearer:
                    authorizationGrant = new JwtBearerAuthorizationGrant(jwt);
                    break;

                case GrantType.RefreshToken:
                    authorizationGrant = new RefreshTokenGrant(jwt);
                    break;
            }

            return authorizationGrant;
        }
    }
}
