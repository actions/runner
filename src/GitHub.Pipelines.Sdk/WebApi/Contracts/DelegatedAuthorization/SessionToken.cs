using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class SessionToken
    {
        public Guid ClientId { get; set; }

        public Guid AccessId { get; set; }

        public Guid AuthorizationId { get; set; }

        public Guid HostAuthorizationId { get; set; }

        public Guid UserId { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public string DisplayName { get; set; }

        public string Scope { get; set; }

        public IList<Guid> TargetAccounts { get; set; }

        /// <summary>
        /// This is computed and not returned in Get queries
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// This is populated when user requests a compact token. The alternate token value is self describing token.
        /// </summary>
        public string AlternateToken { get; set; }

        public bool IsValid { get; set; }

        public bool IsPublic { get; set; }

        public string PublicData { get; set; }

        public string Source { get; set; }

        public IDictionary<String, String> Claims { get; set; }
    }
}
