namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class SessionTokenResult
    {
        public SessionToken SessionToken { get; set; }

        public SessionTokenError SessionTokenError { get; set; }

        public bool HasError => SessionTokenError != SessionTokenError.None;
    }

    public enum SessionTokenError
    {
        None,
        DisplayNameRequired,
        InvalidDisplayName,
        InvalidValidTo,
        InvalidScope,
        UserIdRequired,
        InvalidUserId,
        InvalidUserType,
        AccessDenied,
        FailedToIssueAccessToken,
        InvalidClient,
        InvalidClientType,
        InvalidClientId,
        InvalidTargetAccounts,
        HostAuthorizationNotFound,
        AuthorizationNotFound,
        FailedToUpdateAccessToken,
        SourceNotSupported,
        InvalidSourceIP,
        InvalidSource,
        DuplicateHash,
        SSHPolicyDisabled,
    }

    public enum SessionTokenType
    {
        SelfDescribing,
        Compact
    }
}
