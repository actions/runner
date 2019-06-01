namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public enum ResponseType
    {
        None = 0,
        Assertion = 1,
        IdToken = 2,
        TenantPicker = 3,
        SignoutToken = 4,
        AppToken = 5,
        Code = 6
    }
}
