using System;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class Authorization
    {
        public Guid AuthorizationId { get; set; }
        public Uri RedirectUri { get; set; }
        public Guid IdentityId { get; set; }
        public string Scopes { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset AccessIssued { get; set; }
        public bool IsAccessUsed { get; set; }
        public bool IsValid { get; set; }
        public Guid RegistrationId { get; set; }
        public string Audience { get; set; }
        public string Source { get; set; }
    }
}
