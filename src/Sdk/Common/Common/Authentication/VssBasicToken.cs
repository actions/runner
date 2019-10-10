using System;
using System.Globalization;
using System.Net;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides a token for basic authentication of internet identities.
    /// </summary>
    public sealed class VssBasicToken : IssuedToken
    {
        /// <summary>
        /// Initializes a new <c>BasicAuthToken</c> instance with the specified token value.
        /// </summary>
        /// <param name="credentials">The credentials which should be used for authentication</param>
        public VssBasicToken(ICredentials credentials)
        {
            m_credentials = credentials;
        }

        internal ICredentials Credentials
        {
            get
            {
                return m_credentials;
            }
        }

        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Basic;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            var basicCredential = m_credentials.GetCredential(request.RequestUri, "Basic");
            if (basicCredential != null)
            {
                request.Headers.SetValue(Internal.HttpHeaders.Authorization, "Basic " + FormatBasicAuthHeader(basicCredential));
            }
        }

        private static String FormatBasicAuthHeader(NetworkCredential credential)
        {
            String authHeader = String.Empty;
            if (!String.IsNullOrEmpty(credential.Domain))
            {
                authHeader = String.Format(CultureInfo.InvariantCulture, "{0}\\{1}:{2}", credential.Domain, credential.UserName, credential.Password);
            }
            else
            {
                authHeader = String.Format(CultureInfo.InvariantCulture, "{0}:{1}", credential.UserName, credential.Password);
            }

            return Convert.ToBase64String(VssHttpRequestSettings.Encoding.GetBytes(authHeader));
        }

        private readonly ICredentials m_credentials;
    }
}
