﻿using Microsoft.VisualStudio.Services.WebApi.Jwt;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
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
