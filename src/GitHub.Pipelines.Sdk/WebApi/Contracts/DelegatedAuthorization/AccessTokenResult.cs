using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Jwt;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [DataContract]
    [ClientIncludeModel]
    public class AccessTokenResult
    {
        [DataMember]
        public Guid AuthorizationId { get; set; }
        [DataMember]
        public JsonWebToken AccessToken { get; set; }
        [DataMember]
        public string TokenType { get; set; }
        [DataMember]
        public DateTime ValidTo { get; set; }
        [DataMember]
        public RefreshTokenGrant RefreshToken { get; set; }

        [DataMember]
        public TokenError AccessTokenError { get; set; }

        [DataMember]
        public bool HasError => AccessTokenError != TokenError.None;

        [DataMember]
        public string ErrorDescription { get; set; }
    }
}
