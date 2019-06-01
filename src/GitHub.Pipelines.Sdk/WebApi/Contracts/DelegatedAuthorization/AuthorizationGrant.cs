using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [KnownType(typeof(RefreshTokenGrant))]
    [KnownType(typeof(JwtBearerAuthorizationGrant))]
    [JsonConverter(typeof(AuthorizationGrantJsonConverter))]
    public abstract class AuthorizationGrant
    {
        public AuthorizationGrant(GrantType grantType)
        {
            if (grantType == GrantType.None)
            {
                throw new ArgumentException("Grant type is required.");
            }

            GrantType = grantType;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public GrantType GrantType { get; private set; }
    }
}
