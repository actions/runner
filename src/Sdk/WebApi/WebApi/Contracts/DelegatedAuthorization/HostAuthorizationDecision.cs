using System;

namespace GitHub.Services.DelegatedAuthorization
{
    public class HostAuthorizationDecision
    {
        public HostAuthorizationError HostAuthorizationError { get; set; }

        public Guid HostAuthorizationId { get; set; }

        public bool HasError
        {
            get { return HostAuthorizationError != HostAuthorizationError.None; }
        }
    }

    public enum HostAuthorizationError
    {
        None,        
        ClientIdRequired,        
        AccessDenied,
        FailedToAuthorizeHost,
        ClientIdNotFound,
        InvalidClientId
    }
}
