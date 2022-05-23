using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    /// <summary>
    /// The type of credentials supported natively by the framework
    /// </summary>
    public enum VssCredentialsType
    {
        Windows = 0,
        Federated = 1,
        Basic = 2,
        ServiceIdentity = 3,
        OAuth = 4,
        S2S = 5,
        Other = 6,
        Aad = 7,
    }

    /// <summary>
    /// Provides the ability to control when to show or hide the credential prompt user interface.
    /// </summary>
    public enum CredentialPromptType
    {
        /// <summary>
        /// Show the UI only if necessary to obtain credentials.
        /// </summary>
        PromptIfNeeded = 0,

        /// <summary>
        /// Never show the UI, even if an error occurs.
        /// </summary>
        DoNotPrompt = 2,
    }

    /// <summary>
    /// Provides credentials to use when connecting to a Visual Studio Service.
    /// </summary>
    public class VssCredentials
    {
        /// <summary>
        /// Initializes a new <c>VssCredentials</c> instance with default credentials.
        /// </summary>
        public VssCredentials()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssCredentials</c> instance with the specified windows and issued token 
        /// credential.
        /// </summary>
        /// <param name="federatedCredential">The federated credential to use for authentication</param>
        public VssCredentials(FederatedCredential federatedCredential)
            : this(federatedCredential, EnvironmentUserInteractive
                  ? CredentialPromptType.PromptIfNeeded : CredentialPromptType.DoNotPrompt)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssCredentials</c> instance with the specified windows and issued token 
        /// credential.
        /// </summary>
        /// <param name="federatedCredential">The federated credential to use for authentication</param>
        /// <param name="promptType">CredentialPromptType.PromptIfNeeded if interactive prompts are allowed, otherwise CredentialProptType.DoNotPrompt</param>
        public VssCredentials(
            FederatedCredential federatedCredential,
            CredentialPromptType promptType)
            : this(federatedCredential, promptType, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssCredentials</c> instance with the specified windows and issued token 
        /// credential.
        /// </summary>
        /// <param name="federatedCredential">The federated credential to use for authentication</param>
        /// <param name="promptType">CredentialPromptType.PromptIfNeeded if interactive prompts are allowed; otherwise, CredentialProptType.DoNotPrompt</param>
        /// <param name="scheduler">An optional <c>TaskScheduler</c> to ensure credentials prompting occurs on the UI thread</param>
        public VssCredentials(
            FederatedCredential federatedCredential,
            CredentialPromptType promptType,
            TaskScheduler scheduler)
            : this(federatedCredential, promptType, scheduler, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssCredentials</c> instance with the specified windows and issued token 
        /// credential.
        /// </summary>
        /// <param name="federatedCredential">The federated credential to use for authentication</param>
        /// <param name="promptType">CredentialPromptType.PromptIfNeeded if interactive prompts are allowed; otherwise, CredentialProptType.DoNotPrompt</param>
        /// <param name="scheduler">An optional <c>TaskScheduler</c> to ensure credentials prompting occurs on the UI thread</param>
        /// <param name="credentialPrompt">An optional <c>IVssCredentialPrompt</c> to perform prompting for credentials</param>
        public VssCredentials(
            FederatedCredential federatedCredential,
            CredentialPromptType promptType,
            TaskScheduler scheduler,
            IVssCredentialPrompt credentialPrompt)
        {
            this.PromptType = promptType;

            if (promptType == CredentialPromptType.PromptIfNeeded && scheduler == null)
            {
                // If we use TaskScheduler.FromCurrentSynchronizationContext() here and this is executing under the UI 
                // thread, for example from an event handler in a WinForms applications, this TaskScheduler will capture 
                // the UI SyncrhonizationContext whose MaximumConcurrencyLevel is 1 and only has a single thread to 
                // execute queued work. Then, if the UI thread invokes one of our synchronous methods that are just 
                // wrappers that block until the asynchronous overload returns, and if the async Task queues work to 
                // this TaskScheduler, like GitHub.Services.CommonGetTokenOperation.GetTokenAsync does, 
                // this will produce an immediate deadlock. It is a much safer choice to use TaskScheduler.Default here 
                // as it uses the .NET Framework ThreadPool to execute queued work. 
                scheduler = TaskScheduler.Default;
            }

            if (federatedCredential != null)
            {
                m_federatedCredential = federatedCredential;
                m_federatedCredential.Scheduler = scheduler;
                m_federatedCredential.Prompt = credentialPrompt;
            }

            m_thisLock = new object();
        }

        /// <summary>
        /// Implicitly converts a <c>FederatedCredential</c> instance into a <c>VssCredentials</c> instance.
        /// </summary>
        /// <param name="credential">The federated credential instance</param>
        /// <returns>A new <c>VssCredentials</c> instance which wraps the specified credential</returns>
        public static implicit operator VssCredentials(FederatedCredential credential)
        {
            return new VssCredentials(credential);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not interactive prompts are allowed.
        /// </summary>
        public CredentialPromptType PromptType
        {
            get
            {
                return m_promptType;
            }
            set
            {
                if (value == CredentialPromptType.PromptIfNeeded && !EnvironmentUserInteractive)
                {
                    throw new ArgumentException(CommonResources.CannotPromptIfNonInteractive(), "PromptType");
                }

                m_promptType = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the issued token credentials to use for authentication with the server.
        /// </summary>
        public FederatedCredential Federated
        {
            get
            {
                return m_federatedCredential;
            }
        }

        /// <summary>
        /// A pluggable credential store.
        /// Simply assign a storage implementation to this property
        /// and the <c>VssCredentials</c> will use it to store and retrieve tokens
        /// during authentication.
        /// </summary>
        public IVssCredentialStorage Storage
        {
            get
            {
                return m_credentialStorage;
            }
            set
            {
                m_credentialStorage = value;

                if (m_federatedCredential != null)
                {
                    m_federatedCredential.Storage = value;
                }
            }
        }

        /// <summary>
        ///Attempts to find appropriate Access token for IDE user and add to prompt's parameter
        /// Actual implementation in override.
        /// </summary>
        internal virtual bool TryGetValidAdalToken(IVssCredentialPrompt prompt)
        {
            return false;
        }

        /// <summary>
        /// Creates a token provider for the configured issued token credentials.
        /// </summary>
        /// <param name="serverUrl">The targeted server</param>
        /// <param name="webResponse">The failed web response</param>
        /// <param name="failedToken">The failed token</param>
        /// <returns>A provider for retrieving tokens for the configured credential</returns>
        internal IssuedTokenProvider CreateTokenProvider(
            Uri serverUrl,
            IHttpResponse webResponse,
            IssuedToken failedToken)
        {
            ArgumentUtility.CheckForNull(serverUrl, "serverUrl");

            IssuedTokenProvider tokenProvider = null;
            VssTraceActivity traceActivity = VssTraceActivity.Current;
            lock (m_thisLock)
            {
                tokenProvider = m_currentProvider;
                if (tokenProvider == null || !tokenProvider.IsAuthenticationChallenge(webResponse))
                {
                    // Prefer federated authentication over Windows authentication.
                    if (m_federatedCredential != null && m_federatedCredential.IsAuthenticationChallenge(webResponse))
                    {
                        if (tokenProvider != null)
                        {
                            VssHttpEventSource.Log.IssuedTokenProviderRemoved(traceActivity, tokenProvider);
                        }

                        // TODO: This needs to be refactored or renamed to be more generic ...
                        this.TryGetValidAdalToken(m_federatedCredential.Prompt);

                        tokenProvider = m_federatedCredential.CreateTokenProvider(serverUrl, webResponse, failedToken);

                        if (tokenProvider != null)
                        {
                            VssHttpEventSource.Log.IssuedTokenProviderCreated(traceActivity, tokenProvider);
                        }
                    }

                    m_currentProvider = tokenProvider;
                }

                return tokenProvider;
            }
        }

        /// <summary>
        /// Retrieves the token provider for the provided server URL if one has been created.
        /// </summary>
        /// <param name="serverUrl">The targeted server</param>
        /// <param name="provider">Stores the active token provider, if one exists</param>
        /// <returns>True if a token provider was found, false otherwise</returns>
        public bool TryGetTokenProvider(
            Uri serverUrl,
            out IssuedTokenProvider provider)
        {
            ArgumentUtility.CheckForNull(serverUrl, "serverUrl");

            lock (m_thisLock)
            {
                // Ensure that we attempt to use the most appropriate authentication mechanism by default.
                if (m_currentProvider == null)
                {
                    if (m_federatedCredential != null)
                    {
                        m_currentProvider = m_federatedCredential.CreateTokenProvider(serverUrl, null, null);
                    }

                    if (m_currentProvider != null)
                    {
                        VssHttpEventSource.Log.IssuedTokenProviderCreated(VssTraceActivity.Current, m_currentProvider);
                    }
                }

                provider = m_currentProvider;
            }

            return provider != null;
        }

        /// <summary>
        /// Determines if the web response is an authentication redirect for issued token providers.
        /// </summary>
        /// <param name="webResponse">The web response</param>
        /// <returns>True if this is an token authentication redirect, false otherwise</returns>
        internal bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            bool isChallenge = false;
            if (!isChallenge && m_federatedCredential != null)
            {
                isChallenge = m_federatedCredential.IsAuthenticationChallenge(webResponse);
            }

            return isChallenge;
        }

        internal void SignOut(
            Uri serverUrl,
            Uri serviceLocation,
            string identityProvider)
        {
            // Remove the token in the storage and the current token provider. Note that we don't
            // call InvalidateToken here because we want to remove the whole token not just its value
            if ((m_currentProvider != null) && (m_currentProvider.CurrentToken != null))
            {
                if (m_currentProvider.Credential.Storage != null && m_currentProvider.TokenStorageUrl != null)
                {
                    m_currentProvider.Credential.Storage.RemoveToken(m_currentProvider.TokenStorageUrl, m_currentProvider.CurrentToken);
                }
                m_currentProvider.CurrentToken = null;
            }

            // We need to make sure that the current provider actually supports the signout method
            ISupportSignOut tokenProviderWithSignOut = m_currentProvider as ISupportSignOut;
            if (tokenProviderWithSignOut == null)
            {
                return;
            }

            // Replace the parameters from the service location
            if (serviceLocation != null)
            {
                string serviceLocationUri = serviceLocation.AbsoluteUri;
                serviceLocationUri = serviceLocationUri.Replace("{mode}", "SignOut");
                serviceLocationUri = serviceLocationUri.Replace("{redirectUrl}", serverUrl.AbsoluteUri);
                serviceLocation = new Uri(serviceLocationUri);
            }

            // Now actually signout of the token provider
            tokenProviderWithSignOut.SignOut(serviceLocation, serverUrl, identityProvider);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteAuthorizationToken(
            string token,
            IDictionary<string, string> attributes)
        {
            int i = 0;
            for (int j = 0; j < token.Length; i++, j += 128)
            {
                attributes["AuthTokenSegment" + i] = token.Substring(j, Math.Min(128, token.Length - j));
            }

            attributes["AuthTokenSegmentCount"] = i.ToString(CultureInfo.InvariantCulture);
        }

        protected static string ReadAuthorizationToken(IDictionary<string, string> attributes)
        {
            string authTokenCountValue;
            if (attributes.TryGetValue("AuthTokenSegmentCount", out authTokenCountValue))
            {
                int authTokenCount = int.Parse(authTokenCountValue, CultureInfo.InvariantCulture);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < authTokenCount; i++)
                {
                    string segmentName = "AuthTokenSegment" + i;

                    string segmentValue;
                    if (attributes.TryGetValue(segmentName, out segmentValue))
                    {
                        sb.Append(segmentValue);
                    }
                }

                return sb.ToString();
            }

            return string.Empty;
        }

        protected static bool EnvironmentUserInteractive
        {
            get
            {
                return Environment.UserInteractive;
            }
        }

        private object m_thisLock;
        private CredentialPromptType m_promptType;
        private IssuedTokenProvider m_currentProvider;
        protected FederatedCredential m_federatedCredential;
        private IVssCredentialStorage m_credentialStorage;
    }
}
