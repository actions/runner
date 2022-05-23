using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides functionality to acquire access tokens for OAuth 2.0.
    /// </summary>
    public class VssOAuthTokenProvider : IssuedTokenProvider
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenProvider</c> instance for the specified credential.
        /// </summary>
        /// <param name="credential">The <c>VssOAuthCredential</c> instance which owns the token provider</param>
        /// <param name="serverUrl">The resource server which issued the authentication challenge</param>
        public VssOAuthTokenProvider(
            VssOAuthCredential credential,
            Uri serverUrl)
            : this(credential, serverUrl, credential.AuthorizationUrl, credential.Grant, credential.ClientCredential, credential.TokenParameters)
        {
            m_credential = credential;
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthTokenProvider</c> instance for the specified credential.
        /// </summary>
        /// <param name="credential">The <c>VssOAuthCredential</c> instance which owns the token provider</param>
        /// <param name="serverUrl">The resource server which issued the authentication challenge</param>
        /// <param name="authorizationUrl">The authorization server token endpoint</param>
        /// <param name="grant">The authorization grant to use for token requests</param>
        /// <param name="clientCrential">The client credentials to use for token requests</param>
        /// <param name="tokenParameters">Additional parameters to include with token requests </param>
        protected VssOAuthTokenProvider(
            IssuedTokenCredential credential,
            Uri serverUrl,
            Uri authorizationUrl,
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCrential,
            VssOAuthTokenParameters tokenParameters)
            : base(credential, serverUrl, authorizationUrl)
        {
            m_grant = grant;
            m_tokenParameters = tokenParameters;
            m_clientCredential = clientCrential;
        }

        /// <summary>
        /// Gets the authorization grant for the token provider.
        /// </summary>
        public VssOAuthGrant Grant
        {
            get
            {
                return m_grant;
            }
        }

        /// <summary>
        /// Gets the client credentials for the token provider.
        /// </summary>
        public VssOAuthClientCredential ClientCredential
        {
            get
            {
                return m_clientCredential;
            }
        }

        /// <summary>
        /// Gets the additional parameters configured for the token provider.
        /// </summary>
        public VssOAuthTokenParameters TokenParameters
        {
            get
            {
                return m_tokenParameters;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not this token provider requires interactivity.
        /// </summary>
        public override Boolean GetTokenIsInteractive
        {
            get
            {
                return false;
            }
        }

        protected override String AuthenticationParameter
        {
            get
            {
                if (this.ClientCredential == null)
                {
                    return null;
                }
                else
                {
                    return String.Format(CultureInfo.InvariantCulture, "client_id=\"{0}\" audience=\"{1}\"", this.ClientCredential.ClientId, this.SignInUrl.AbsoluteUri);
                }
            }
        }

        protected override String AuthenticationScheme
        {
            get
            {
                return "Bearer";
            }
        }

        public async Task<string> ValidateCredentialAsync(CancellationToken cancellationToken)
        {
            var tokenHttpClient = new VssOAuthTokenHttpClient(this.SignInUrl);
            var tokenResponse = await tokenHttpClient.GetTokenAsync(this.Grant, this.ClientCredential, this.TokenParameters, cancellationToken);

            // return the underlying authentication error
            return tokenResponse.Error;
        }

        /// <summary>
        /// Issues a token request to the configured secure token service. On success, the access token issued by the 
        /// token service is returned to the caller
        /// </summary>
        /// <param name="failedToken">If applicable, the previous token which is now considered invalid</param>
        /// <param name="cancellationToken">A token used for signalling cancellation</param>
        /// <returns>A <c>Task&lgt;IssuedToken&gt;</c> for tracking the progress of the token request</returns>
        protected override async Task<IssuedToken> OnGetTokenAsync(
            IssuedToken failedToken,
            CancellationToken cancellationToken)
        {
            if (this.SignInUrl == null ||
                this.Grant == null ||
                this.ClientCredential == null)
            {
                return null;
            }

            IssuedToken issuedToken = null;
            var traceActivity = VssTraceActivity.Current;
            try
            {
                var tokenHttpClient = new VssOAuthTokenHttpClient(this.SignInUrl);
                var tokenResponse = await tokenHttpClient.GetTokenAsync(this.Grant, this.ClientCredential, this.TokenParameters, cancellationToken).ConfigureAwait(false);
                if (!String.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    // Construct a new access token based on the response, including the expiration time so we know 
                    // when to refresh the token.
                    issuedToken = CreateIssuedToken(tokenResponse);

                    if (!String.IsNullOrEmpty(tokenResponse.RefreshToken))
                    {
                        // TODO: How should this flow be handled? Refresh Token is a credential change which is not 
                        //       the same thing as access token storage
                    }
                }
                else if (!String.IsNullOrEmpty(tokenResponse.Error))
                {
                    // Raise a new exception describing the underlying authentication error
                    throw new VssOAuthTokenRequestException(tokenResponse.ErrorDescription)
                    {
                        Error = tokenResponse.Error,
                    };
                }
                else
                {
                    // If the error property isn't set, but we didn't get an access token, then it's not
                    // clear what the issue is. In this case just trace the response and fall through with
                    // a null access token return value.
                    var sb = new StringBuilder();
                    var serializer = JsonSerializer.Create(s_traceSettings.Value);
                    using (var sr = new StringWriter(sb))
                    {
                        serializer.Serialize(sr, tokenResponse);
                    }

                    VssHttpEventSource.Log.AuthenticationError(traceActivity, this, sb.ToString());
                }
            }
            catch (VssServiceResponseException ex)
            {
                VssHttpEventSource.Log.AuthenticationError(traceActivity, this, ex);
            }

            return issuedToken;
        }

        protected virtual IssuedToken CreateIssuedToken(VssOAuthTokenResponse tokenResponse)
        {
            if (tokenResponse.ExpiresIn > 0)
            {
                return new VssOAuthAccessToken(tokenResponse.AccessToken, DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
            }
            else
            {
                return new VssOAuthAccessToken(tokenResponse.AccessToken);
            }
        }

        private static JsonSerializerSettings CreateTraceSettings()
        {
            var settings = new VssJsonMediaTypeFormatter().SerializerSettings;
            settings.Formatting = Formatting.Indented;
            return settings;
        }

        private readonly VssOAuthGrant m_grant;
        private readonly VssOAuthCredential m_credential;
        private readonly VssOAuthTokenParameters m_tokenParameters;
        private readonly VssOAuthClientCredential m_clientCredential;
        private static readonly Lazy<JsonSerializerSettings> s_traceSettings = new Lazy<JsonSerializerSettings>(CreateTraceSettings);
    }
}
