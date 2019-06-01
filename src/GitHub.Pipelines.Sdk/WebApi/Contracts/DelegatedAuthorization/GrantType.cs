namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public enum GrantType
    {
        None = 0,
        JwtBearer = 1,
        RefreshToken = 2,
        Implicit = 3,
        ClientCredentials = 4,
    }
}
