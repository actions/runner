using Microsoft.VisualStudio.Services.WebApi.Jwt;
using System;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class AccessToken
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Guid AccessId { get; set; }

        public Guid AuthorizationId { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTimeOffset Refreshed { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsRefresh { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsValid { get; set; }

        public JsonWebToken Token { get; set; }
        public string TokenType { get; set; }
        public JsonWebToken RefreshToken { get; set; }
    }
}
