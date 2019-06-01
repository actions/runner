namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public enum AuthorizationError
    {
        None,
        ClientIdRequired,
        InvalidClientId,
        ResponseTypeRequired,
        ResponseTypeNotSupported,
        ScopeRequired,
        InvalidScope,
        RedirectUriRequired,
        InsecureRedirectUri,
        InvalidRedirectUri,
        InvalidUserId,
        InvalidUserType,
        AccessDenied
    }
}
