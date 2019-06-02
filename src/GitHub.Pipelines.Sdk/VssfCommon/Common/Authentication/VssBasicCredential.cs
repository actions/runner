using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.Services.Common.Internal;

namespace Microsoft.VisualStudio.Services.Common
{
    /// <summary>
    /// Provides a credential for basic authentication against a Visual Studio Service.
    /// </summary>
    public sealed class VssBasicCredential : FederatedCredential
    {
        /// <summary>
        /// Initializes a new <c>VssBasicCredential</c> instance with no token specified.
        /// </summary>
        public VssBasicCredential()
            : this((VssBasicToken)null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssBasicCredential</c> instance with the specified user name and password.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        public VssBasicCredential(
            string userName,
            string password)
            : this(new VssBasicToken(new NetworkCredential(userName, password)))
        {
        }

        /// <summary>
        /// Initializes a new <c>VssBasicCredential</c> instance with the specified token.
        /// </summary>
        /// <param name="initialToken">An optional token which, if present, should be used before obtaining a new token</param>
        public VssBasicCredential(ICredentials initialToken)
            : this(new VssBasicToken(initialToken))
        {
        }

        /// <summary>
        /// Initializes a new <c>VssBasicCredential</c> instance with the specified token.
        /// </summary>
        /// <param name="initialToken">An optional token which, if present, should be used before obtaining a new token</param>
        public VssBasicCredential(VssBasicToken initialToken)
            : base(initialToken)
        {
        }

        public override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Basic;
            }
        }

        public override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            if (webResponse.StatusCode != HttpStatusCode.Found &&
                webResponse.StatusCode != HttpStatusCode.Redirect &&
                webResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            return webResponse.Headers.GetValues(HttpHeaders.WwwAuthenticate).Any(x => x.StartsWith("Basic", StringComparison.OrdinalIgnoreCase));
        }

        protected override IssuedTokenProvider OnCreateTokenProvider(
            Uri serverUrl,
            IHttpResponse response)
        {
            if (serverUrl.Scheme != "https")
            {
                String unsafeBasicAuthEnv = Environment.GetEnvironmentVariable("VSS_ALLOW_UNSAFE_BASICAUTH") ?? "false";
                if (!Boolean.TryParse(unsafeBasicAuthEnv, out Boolean unsafeBasicAuth) || !unsafeBasicAuth)
                {
                    throw new InvalidOperationException(CommonResources.BasicAuthenticationRequiresSsl());
                }
            }

            return new BasicAuthTokenProvider(this, serverUrl);
        }
    }
}
