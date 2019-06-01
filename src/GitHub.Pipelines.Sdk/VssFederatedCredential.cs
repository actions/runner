using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Common.Internal;

namespace Microsoft.VisualStudio.Services.Client
{
    /// <summary>
    /// Provides federated authentication with a hosted <c>VssConnection</c> instance using cookies.
    /// </summary>
    [Serializable]
    public sealed class VssFederatedCredential : FederatedCredential
    {
        /// <summary>
        /// Initializes a new <c>VssFederatedCredential</c> instance.
        /// </summary>
        public VssFederatedCredential()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssFederatedCredential</c> instance.
        /// </summary>
        public VssFederatedCredential(Boolean useCache)
            : this(useCache, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssFederatedCredential</c> instance.
        /// </summary>
        /// <param name="initialToken">The initial token if available</param>
        public VssFederatedCredential(VssFederatedToken initialToken)
            : this(false, initialToken)
        {
        }

        public VssFederatedCredential(
            Boolean useCache,
            VssFederatedToken initialToken)
            : base(initialToken)
        {
#if !NETSTANDARD
            if (useCache)
            {
                Storage = new VssClientCredentialStorage();
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public override VssCredentialsType CredentialType
        {
            get 
            { 
                return VssCredentialsType.Federated; 
            }
        }

        public override Boolean IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            bool isNonAuthenticationChallenge = false;
            return IsVssFederatedAuthenticationChallenge(webResponse, out isNonAuthenticationChallenge) ?? isNonAuthenticationChallenge;
        }

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
            String realm = String.Empty;
            String issuer = String.Empty;

            if (response != null)
            {
                var location = response.Headers.GetValues(HttpHeaders.Location).FirstOrDefault();
                if (location == null)
                {
                    location = response.Headers.GetValues(HttpHeaders.TfsFedAuthRedirect).FirstOrDefault();
                }

                if (!String.IsNullOrEmpty(location))
                {
                    signInUrl = new Uri(location);
                }

                // Inform the server that we support the javascript notify "smart client" pattern for ACS auth
                AddParameter(ref signInUrl, "protocol", "javascriptnotify");

                // Do not automatically sign in with existing FedAuth cookie
                AddParameter(ref signInUrl, "force", "1");

                GetRealmAndIssuer(response, out realm, out issuer);
            }

            return new VssFederatedTokenProvider(this, serverUrl, signInUrl, issuer, realm);
        }

        internal static void GetRealmAndIssuer(
            IHttpResponse response, 
            out String realm,
            out String issuer)
        {
            realm = response.Headers.GetValues(HttpHeaders.TfsFedAuthRealm).FirstOrDefault();
            issuer = response.Headers.GetValues(HttpHeaders.TfsFedAuthIssuer).FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(issuer))
            {
                issuer = new Uri(issuer).GetLeftPart(UriPartial.Authority);
            }
        }

        internal static Boolean? IsVssFederatedAuthenticationChallenge(
            IHttpResponse webResponse, 
            out Boolean isNonAuthenticationChallenge)
        {
            isNonAuthenticationChallenge = false;

            if (webResponse == null)
            {
                return false;
            }

            // Check to make sure that the redirect was issued from the Tfs service. We include the TfsServiceError
            // header to avoid the possibility that a redirect from a non-tfs service is issued and we incorrectly
            // launch the credentials UI.
            if (webResponse.StatusCode == HttpStatusCode.Found ||
                webResponse.StatusCode == HttpStatusCode.Redirect)
            {
                return webResponse.Headers.GetValues(HttpHeaders.Location).Any() && webResponse.Headers.GetValues(HttpHeaders.TfsFedAuthRealm).Any();
            }
            else if (webResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return webResponse.Headers.GetValues(HttpHeaders.WwwAuthenticate).Any(x => x.StartsWith("TFS-Federated", StringComparison.OrdinalIgnoreCase));
            }
            else if (webResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                // This is not strictly an "authentication challenge" but it is a state the user can do something about so they can get access to the resource
                // they are attempting to access. Specifically, the user will hit this when they need to update or create a profile required by business policy.
                isNonAuthenticationChallenge = webResponse.Headers.GetValues(HttpHeaders.TfsFedAuthRedirect).Any();
                if (isNonAuthenticationChallenge)
                {
                    return null;
                }
            }

            return false;
        }

        private static void AddParameter(ref Uri uri, String name, String value)
        {
            if (uri.Query.IndexOf(String.Concat(name, "="), StringComparison.OrdinalIgnoreCase) < 0)
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Query = String.Concat(builder.Query.TrimStart('?'), "&", name, "=", value);
                uri = builder.Uri;
            }
        }
    }
}
