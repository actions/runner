using System;
using System.Runtime.Serialization;

namespace GitHub.Services.OAuth
{
    [DataContract]
    public sealed class AccessTokenResponse
    {
        public AccessTokenResponse()
        {
        }

        public AccessTokenResponse(
            String accessToken,
            String expiresIn,
            String refreshToken)
        {
            AccessToken = accessToken;
            TokenType = "bearer";
            ExpiresIn = expiresIn;
            RefreshToken = refreshToken;
        }

        [DataMember(Name = "access_token")]
        public String AccessToken { get; set; }

        [DataMember(Name = "token_type")]
        public String TokenType { get; set; }

        [DataMember(Name = "expires_in")]
        public String ExpiresIn { get; set; }

        [DataMember(Name = "refresh_token")]
        public String RefreshToken { get; set; }
    }
}
