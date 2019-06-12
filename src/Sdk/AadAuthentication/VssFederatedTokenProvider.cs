using System;
using System.Net;
using System.Net.Http;
using GitHub.Services.Common;
using System.Globalization;

namespace GitHub.Services.Client
{
    /// <summary>
    /// Provides authentication for internet identities using single-sign-on cookies.
    /// </summary>
    internal sealed class VssFederatedTokenProvider : IssuedTokenProvider, ISupportSignOut
    {
        public VssFederatedTokenProvider(
            VssFederatedCredential credential,
            Uri serverUrl,
            Uri signInUrl,
            String issuer, 
            String realm)
            : base(credential, serverUrl, signInUrl)
        {
            Issuer = issuer;
            Realm = realm;
        }

        protected override String AuthenticationScheme
        {
            get 
            {
                return "TFS-Federated";
            }
        }

        protected override String AuthenticationParameter
        {
            get
            {
                if (String.IsNullOrEmpty(this.Issuer) && String.IsNullOrEmpty(this.Realm))
                {
                    return String.Empty;
                }
                else
                {
                    return String.Format(CultureInfo.InvariantCulture, "issuer=\"{0}\", realm=\"{1}\"", this.Issuer, this.Realm);
                }
            }
        }

        /// <summary>
        /// Gets the federated credential from which this provider was created.
        /// </summary>
        public new VssFederatedCredential Credential
        {
            get
            {
                return (VssFederatedCredential)base.Credential;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not a call to get token will require interactivity.
        /// </summary>
        public override Boolean GetTokenIsInteractive
        {
            get
            {
                return this.CurrentToken == null;
            }
        }

        /// <summary>
        /// Gets the issuer for the token provider.
        /// </summary>
        public String Issuer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the realm for the token provider.
        /// </summary>
        public String Realm
        {
            get;
            private set;
        }

        protected internal override Boolean IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (!base.IsAuthenticationChallenge(webResponse))
            {
                return false;
            }

            // This means we were proactively constructed without any connection information. In this case
            // we return false to ensure that a new provider is reconstructed with all appropriate configuration
            // to retrieve a new token.
            if (this.SignInUrl == null)
            {
                return false;
            }

            String realm, issuer;
            VssFederatedCredential.GetRealmAndIssuer(webResponse, out realm, out issuer);

            return this.Realm.Equals(realm, StringComparison.OrdinalIgnoreCase) &&
                   this.Issuer.Equals(issuer, StringComparison.OrdinalIgnoreCase);
        }

        protected override IssuedToken OnValidatingToken(
            IssuedToken token, 
            IHttpResponse webResponse)
        {
            // If the response has Set-Cookie headers, attempt to retrieve the FedAuth cookie from the response
            // and replace the current token with the new FedAuth cookie. Note that the server only reissues the
            // FedAuth cookie if it is issued for more than an hour.
            CookieCollection fedAuthCookies = CookieUtility.GetFederatedCookies(webResponse);

            if (fedAuthCookies != null) 
            {
                // The reissued token should have the same user information as the previous one.
                VssFederatedToken federatedToken = new VssFederatedToken(fedAuthCookies)
                {
                    Properties = token.Properties,
                    UserId = token.UserId,
                    UserName = token.UserName
                };

                token = federatedToken;
            }

            return token;
        }

        public void SignOut(Uri signOutUrl, Uri replyToUrl, String identityProvider)
        {
            // The preferred implementation is to follow the signOutUrl with a browser and kill the browser whenever it
            // arrives at the replyToUrl (or if it bombs out somewhere along the way).
            // This will work for all Web-based identity providers (Live, Google, Yahoo, Facebook) supported by ACS provided that
            // the TFS server has registered sign-out urls (in the TF Registry) for each of these.
            // This is the long-term approach that should be pursued and probably the approach recommended to other
            // clients which don't have direct access to the cookie store (TEE?)

            // In the short term we are simply going to delete the TFS cookies and the Windows Live cookies that are exposed to this
            // session. This has the drawback of not properly signing out of Live (you'd still be signed in to e.g. Hotmail, Xbox, MSN, etc.)
            // but will allow the user to re-enter their live credentials and sign-in again to TFS.
            // The other drawback is that the clients will have to be updated again when we pursue the implementation outlined above.

            CookieUtility.DeleteFederatedCookies(replyToUrl);
            if (!String.IsNullOrEmpty(identityProvider) && identityProvider.Equals("Windows Live ID", StringComparison.OrdinalIgnoreCase))
            {
                CookieUtility.DeleteWindowsLiveCookies();
            }
        }
    }
}
