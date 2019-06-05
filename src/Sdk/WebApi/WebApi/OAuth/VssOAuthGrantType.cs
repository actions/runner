namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Lists the supported authorization grant types
    /// </summary>
    public enum VssOAuthGrantType
    {
        /// <summary>
        /// Authorization Code Grant for OAuth 2.0
        /// </summary>
        AuthorizationCode,

        /// <summary>
        /// Client Credentials Grant for OAuth 2.0
        /// </summary>
        ClientCredentials,

        /// <summary>
        /// JWT Bearer Token Grant Type Profile for OAuth 2.0
        /// </summary>
        JwtBearer,

        /// <summary>
        /// Refresh Token Grant for OAuth 2.0
        /// </summary>
        RefreshToken,
    }
}
