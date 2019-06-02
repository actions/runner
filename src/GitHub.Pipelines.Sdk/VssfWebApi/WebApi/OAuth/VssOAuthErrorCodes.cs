using System;

namespace Microsoft.VisualStudio.Services.OAuth
{
    /// <summary>
    /// Provides the possible error codes which can result from a bad token exchange in OAuth 2.0.
    /// </summary>
    public static class VssOAuthErrorCodes
    {
        /// <summary>
        /// The resource owner or authorization server denied the request.
        /// </summary>
        public static readonly String AccessDenied = "access_denied";

        /// <summary>
        /// Client authentication failed (e.g. unknown client, no client authentication included, or unsupported
        /// authentication method).
        /// </summary>
        public static readonly String InvalidClient = "invalid_client";

        /// <summary>
        /// The provided authorization grant is invalid, expired, revoked, or does not match the redirection URI used 
        /// in the authorization request, or was issued to another client.
        /// </summary>
        public static readonly String InvalidGrant = "invalid_grant";

        /// <summary>
        /// The request is missing a required parameter.
        /// </summary>
        public static readonly String InvalidRequest = "invalid_request";

        /// <summary>
        /// The requested scope is invalid, unknown, malformed, or exceeds the scope granted by the resource owner.
        /// </summary>
        public static readonly String InvalidScope = "invalid_scope";

        /// <summary>
        /// The authorization server encountered an unexpected condition that prevented it from fulfilling the request.
        /// </summary>
        public static readonly String ServerError = "server_error";

        /// <summary>
        /// The authorization server is currently unable to handle the request due to temporary orverloading or 
        /// maintenance of the server.
        /// </summary>
        public static readonly String TemporarilyUnavailable = "temporarily_unavailable";

        /// <summary>
        /// The authenticated client is not authorized to use this authorization grant type.
        /// </summary>
        public static readonly String UnauthorizedClient = "unauthorized_client";

        /// <summary>
        /// The authorization grant type is not supported by the authorization server.
        /// </summary>
        public static readonly String UnsupportedGrantType = "unsupported_grant_type";

        /// <summary>
        /// The authorization server does not support obtaining access tokens using this method.
        /// </summary>
        public static readonly String UnsupportedResponseType = "unsupported_response_type";
    }
}
