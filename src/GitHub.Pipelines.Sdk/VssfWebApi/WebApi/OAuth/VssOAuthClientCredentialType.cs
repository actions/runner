namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Lists the supported client credential types
    /// </summary>
    public enum VssOAuthClientCredentialType
    {
        /// <summary>
        /// Client Password for OAuth 2.0 Client Authentication
        /// </summary>
        Password,

        /// <summary>
        /// JWT Bearer Token Profile for OAuth 2.0 Client Authentication
        /// </summary>
        JwtBearer,
    }
}
