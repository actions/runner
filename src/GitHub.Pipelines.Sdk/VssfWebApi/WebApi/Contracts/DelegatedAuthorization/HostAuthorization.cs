using System;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class HostAuthorization
    {
        public Guid Id { get; set; }
        public Guid RegistrationId { get; set; }
        public Guid HostId { get; set; }
        public bool IsValid { get; set; }
    }
}
