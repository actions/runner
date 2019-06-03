namespace GitHub.Services.DelegatedAuthorization
{
    public enum InitiationError
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
        InvalidRedirectUri
    }
}
