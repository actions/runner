using Microsoft.VisualStudio.Services.WebApi.Jwt;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class RefreshTokenGrant : AuthorizationGrant
    {
        public RefreshTokenGrant(JsonWebToken jwt)
            : base(GrantType.RefreshToken)
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
