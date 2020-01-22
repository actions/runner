using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.DelegatedAuthorization
{
    public class JwtBearerAuthorizationGrant : AuthorizationGrant
    {
        public JwtBearerAuthorizationGrant(JsonWebToken jwt)
            : base(GrantType.JwtBearer)
        {
            Jwt = jwt;
        }

        public JsonWebToken Jwt { get; private set; }

        public override string ToString()
        {
            return Jwt.EncodedToken;
        }
    }
}
