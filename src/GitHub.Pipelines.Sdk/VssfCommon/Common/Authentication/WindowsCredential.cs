using System;
using System.Linq;
using System.Net;

namespace Microsoft.VisualStudio.Services.Common
{
    /// <summary>
    /// Provides a credential for windows authentication against a Visual Studio Service.
    /// </summary>
    public sealed class WindowsCredential : IssuedTokenCredential
    {
        /// <summary>
        /// Initializes a new <c>WindowsCredential</c> instance using a default user interface provider implementation 
        /// and the default network credentials.
        /// </summary>
        public WindowsCredential()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new <c>WindowsCredential</c> instance using a default user interface provider implementation 
        /// and the default network credentials, if specified.
        /// </summary>
        /// <param name="useDefaultCredentials">True if the default credentials should be used; otherwise, false</param>
        public WindowsCredential(bool useDefaultCredentials)
            : this(useDefaultCredentials ? CredentialCache.DefaultCredentials : null)
        {
            UseDefaultCredentials = useDefaultCredentials;
        }

        /// <summary>
        /// Initializes a new <c>WindowsCredential</c> instance using a default user interface provider implementation 
        /// and the specified network credentials.
        /// </summary>
        /// <param name="credentials">The windows credentials which should be used for authentication</param>
        public WindowsCredential(ICredentials credentials)
            : this(null)
        {
            m_credentials = credentials;
            UseDefaultCredentials = credentials == CredentialCache.DefaultCredentials;
        }

        /// <summary>
        /// Initializes a new <c>WindowsCredential</c> instance using the specified initial token.
        /// </summary>
        /// <param name="initialToken">An optional token which, if present, should be used before obtaining a new token</param>
        public WindowsCredential(WindowsToken initialToken)
            : base(initialToken)
        {
        }

        /// <summary>
        /// Gets the credentials associated with this windows credential.
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return m_credentials;
            }
            set
            {
                m_credentials = value;
                UseDefaultCredentials = Credentials == CredentialCache.DefaultCredentials;
            }
        }

        public override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Windows;
            }
        }

        /// <summary>
        /// Gets a value indicating what value was passed to WindowsCredential(bool useDefaultCredentials) constructor
        /// </summary>
        public Boolean UseDefaultCredentials
        {
            get;
            private set;
        }

        public override Boolean IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            if (webResponse.StatusCode == HttpStatusCode.Unauthorized &&
                webResponse.Headers.GetValues(Internal.HttpHeaders.WwwAuthenticate).Any(x => AuthenticationSchemeValid(x)))
            {
                return true;
            }

            if (webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired &&
                webResponse.Headers.GetValues(Internal.HttpHeaders.ProxyAuthenticate).Any(x => AuthenticationSchemeValid(x)))
            {
                return true;
            }

            return false;
        }

        protected override IssuedTokenProvider OnCreateTokenProvider(
            Uri serverUrl, 
            IHttpResponse response)
        {
            // If we have no idea what kind of credentials we are supposed to be using, don't play a windows token on
            // the first request.
            if (response == null)
            {
                return null;
            }

            if (m_credentials != null)
            {
                this.InitialToken = new WindowsToken(m_credentials);
            }

            return new WindowsTokenProvider(this, serverUrl);
        }

        private static Boolean AuthenticationSchemeValid(String authenticateHeader)
        {
            return authenticateHeader.StartsWith("Basic", StringComparison.OrdinalIgnoreCase) ||
                   authenticateHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase) ||
                   authenticateHeader.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase) ||
                   authenticateHeader.StartsWith("Ntlm", StringComparison.OrdinalIgnoreCase);
        }

        private ICredentials m_credentials;
    }
}
