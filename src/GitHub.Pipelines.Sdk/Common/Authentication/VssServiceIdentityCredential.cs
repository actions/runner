using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Microsoft.VisualStudio.Services.Common
{
    /// <summary>
    /// Provides federated authentication as a service identity with a Visual Studio Service.
    /// </summary>
    [Serializable]
    public sealed class VssServiceIdentityCredential : FederatedCredential
    {
        /// <summary>
        /// Initializes a new <c>VssServiceIdentityCredential</c> instance with the specified user name and password.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        public VssServiceIdentityCredential(
            string userName,
            string password)
            : this(userName, password, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssServiceIdentityCredential</c> instance with the specified user name and password. The
        /// provided token, if not null, will be used before attempting authentication with the credentials.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <param name="initialToken">An optional token which, if present, should be used before obtaining a new token</param>
        public VssServiceIdentityCredential(
            string userName,
            string password,
            VssServiceIdentityToken initialToken)
            : this(userName, password, initialToken, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssServiceIdentityCredential</c> instance with the specified access token.
        /// </summary>
        /// <param name="token">A token which may be used for authorization as the desired service identity</param>
        public VssServiceIdentityCredential(VssServiceIdentityToken token)
            : this(null, null, token, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssServiceIdentityCredential</c> instance with the specified user name and password. The
        /// provided token, if not null, will be used before attempting authentication with the credentials.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <param name="initialToken">An optional token which, if present, should be used before obtaining a new token</param>
        /// <param name="innerHandler">An optional HttpMessageHandler which if passed will be passed along to the TokenProvider when executing OnCreateTokenProvider </param>
        public VssServiceIdentityCredential(
            string userName, 
            string password, 
            VssServiceIdentityToken initialToken,
            DelegatingHandler innerHandler)
            : base(initialToken)
        {
            m_userName = userName;
            m_password = password;
            m_innerHandler = innerHandler;
        }

        public override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.ServiceIdentity;
            }
        }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        public String UserName
        {
            get
            {
                return m_userName;
            }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        internal String Password
        {
            get
            {
                return m_password;
            }
        }

        public override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            if (webResponse.StatusCode == HttpStatusCode.Found ||
                webResponse.StatusCode == HttpStatusCode.Redirect ||
                webResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var authRealm = webResponse.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthRealm).FirstOrDefault();
                var authIssuer = webResponse.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthIssuer).FirstOrDefault();
                var wwwAuthenticate = webResponse.Headers.GetValues(Internal.HttpHeaders.WwwAuthenticate);
                if (!String.IsNullOrEmpty(authIssuer) && !String.IsNullOrEmpty(authRealm))
                {
                    return webResponse.StatusCode != HttpStatusCode.Unauthorized || wwwAuthenticate.Any(x => x.StartsWith("TFS-Federated", StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        internal override string GetAuthenticationChallenge(IHttpResponse webResponse)
        {
            var authRealm = webResponse.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthRealm).FirstOrDefault();
            var authIssuer = webResponse.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthIssuer).FirstOrDefault();
            return string.Format(CultureInfo.InvariantCulture, "TFS-Federated realm={0}, issuer={1}", authRealm, authIssuer);
        }

        /// <summary>
        /// Creates a provider for retrieving security tokens for the provided credentials.
        /// </summary>
        /// <returns>An issued token provider for the current credential</returns>
        protected override IssuedTokenProvider OnCreateTokenProvider(
            Uri serverUrl, 
            IHttpResponse response)
        {
            // The response is only null when attempting to determine the most appropriate token provider to
            // use for the connection. The only way we should do anything here is if we have an initial token
            // since that means we can present something without making a server call.
            if (response == null && base.InitialToken == null)
            {
                return null;
            }

            Uri signInUrl = null;
            String realm = string.Empty;
            if (response != null)
            {
                realm = response.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthRealm).FirstOrDefault();
                signInUrl = new Uri(new Uri(response.Headers.GetValues(Internal.HttpHeaders.TfsFedAuthIssuer).FirstOrDefault()).GetLeftPart(UriPartial.Authority));
            }

            return new VssServiceIdentityTokenProvider(this, serverUrl, signInUrl, realm, m_innerHandler);
        }

        private readonly String m_userName;
        private readonly String m_password;

        [NonSerialized]
        private readonly DelegatingHandler m_innerHandler = null;
    }
}
