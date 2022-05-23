using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides a common base class for issued token credentials.
    /// </summary>
    [Serializable]
    public abstract class IssuedTokenCredential
    {
        protected IssuedTokenCredential(IssuedToken initialToken)
        {
            InitialToken = initialToken;
        }

        public abstract VssCredentialsType CredentialType
        {
            get;
        }

        /// <summary>
        /// The initial token to use to authenticate if available. 
        /// </summary>
        internal IssuedToken InitialToken
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the synchronization context which should be used for UI prompts.
        /// </summary>
        internal TaskScheduler Scheduler
        {
            get
            {
                return m_scheduler;
            }
            set
            {
                m_scheduler = value;
            }
        }

        /// <summary>
        /// The credentials prompt which is used for retrieving a new token.
        /// </summary>
        internal IVssCredentialPrompt Prompt
        {
            get
            {
                return m_prompt;
            }
            set
            {
                m_prompt = value;
            }
        }

        internal IVssCredentialStorage Storage
        {
            get
            {
                return m_storage;
            }
            set
            {
                m_storage = value;
            }
        }

        /// <summary>
        /// The base url for the vssconnection to be used in the token storage key.
        /// </summary>
        internal Uri TokenStorageUrl { get; set; }

        /// <summary>
        /// Creates a token provider suitable for handling the challenge presented in the response.
        /// </summary>
        /// <param name="serverUrl">The targeted server</param>
        /// <param name="response">The challenge response</param>
        /// <param name="failedToken">The failed token</param>
        /// <returns>An issued token provider instance</returns>
        internal IssuedTokenProvider CreateTokenProvider(
            Uri serverUrl,
            IHttpResponse response,
            IssuedToken failedToken)
        {
            if (response != null && !IsAuthenticationChallenge(response))
            {
                throw new InvalidOperationException();
            }

            if (InitialToken == null && Storage != null)
            {
                if (TokenStorageUrl == null)
                {
                    throw new InvalidOperationException($"The {nameof(TokenStorageUrl)} property must have a value if the {nameof(Storage)} property is set on this instance of {GetType().Name}.");
                }
                InitialToken = Storage.RetrieveToken(TokenStorageUrl, CredentialType);
            }

            IssuedTokenProvider provider = OnCreateTokenProvider(serverUrl, response);
            if (provider != null)
            {
                provider.TokenStorageUrl = TokenStorageUrl;
            }

            // If the initial token is the one which failed to authenticate, don't 
            // use it again and let the token provider get a new token.
            if (provider != null)
            {
                if (InitialToken != null && !Object.ReferenceEquals(InitialToken, failedToken))
                {
                    provider.CurrentToken = InitialToken;
                }
            }

            return provider;
        }

        internal virtual string GetAuthenticationChallenge(IHttpResponse webResponse)
        {
            IEnumerable<String> values;
            if (!webResponse.Headers.TryGetValues(Internal.HttpHeaders.WwwAuthenticate, out values))
            {
                return String.Empty;
            }

            return String.Join(", ", values);
        }

        public abstract bool IsAuthenticationChallenge(IHttpResponse webResponse);

        protected abstract IssuedTokenProvider OnCreateTokenProvider(Uri serverUrl, IHttpResponse response);

        [NonSerialized]
        private TaskScheduler m_scheduler;

        [NonSerialized]
        private IVssCredentialPrompt m_prompt;

        [NonSerialized]
        private IVssCredentialStorage m_storage;
    }
}
