using System;

namespace GitHub.Services.DelegatedAuthorization
{
    public class AppSessionTokenResult
    {
        public string AppSessionToken { get; set; }

        public DateTime ExpirationDate { get; set; }

        public AppSessionTokenError AppSessionTokenError { get; set; }

        public bool HasError => AppSessionTokenError != AppSessionTokenError.None;
    }

    public enum AppSessionTokenError
    {
        None,
        UserIdRequired,
        ClientIdRequired,
        InvalidUserId,
        InvalidUserType,
        AccessDenied,
        FailedToIssueAppSessionToken,
        InvalidClientId,
        AuthorizationIsNotSuccessfull
    }
}
