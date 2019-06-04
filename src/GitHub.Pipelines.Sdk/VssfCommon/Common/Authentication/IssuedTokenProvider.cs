using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Diagnostics;

namespace GitHub.Services.Common
{
    internal interface ISupportSignOut
    {
        void SignOut(Uri serverUrl, Uri replyToUrl, string identityProvider);
    }

    /// <summary>
    /// Provides a common base class for providers of the token authentication model.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class IssuedTokenProvider
    {
        private const double c_slowTokenAcquisitionTimeInSeconds = 2.0;

        protected IssuedTokenProvider(
            IssuedTokenCredential credential,
            Uri serverUrl,
            Uri signInUrl)
        {
            ArgumentUtility.CheckForNull(credential, "credential");

            this.SignInUrl = signInUrl;
            this.Credential = credential;
            this.ServerUrl = serverUrl;

            m_thisLock = new object();
        }

        /// <summary>
        /// Gets the authentication scheme used to create this token provider.
        /// </summary>
        protected virtual String AuthenticationScheme
        {
            get
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the authentication parameter or parameters used to create this token provider.
        /// </summary>
        protected virtual String AuthenticationParameter
        {
            get
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the credential associated with the provider.
        /// </summary>
        protected internal IssuedTokenCredential Credential
        {
            get;
        }

        internal VssCredentialsType CredentialType => this.Credential.CredentialType;

        /// <summary>
        /// Gets the current token.
        /// </summary>
        public IssuedToken CurrentToken
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether or not a call to get token will require interactivity.
        /// </summary>
        public abstract bool GetTokenIsInteractive
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether or not an ISynchronizeInvoke call is required.
        /// </summary>
        private Boolean InvokeRequired
        {
            get
            {
                return this.GetTokenIsInteractive && this.Credential.Scheduler != null;
            }
        }

        /// <summary>
        /// Gets the sign-in URL for the token provider.
        /// </summary>
        public Uri SignInUrl { get; private set; }

        protected Uri ServerUrl { get; }

        /// <summary>
        /// The base url for the vssconnection to be used in the token storage key.
        /// </summary>
        internal Uri TokenStorageUrl { get; set; }

        /// <summary>
        /// Determines whether the specified web response is an authentication challenge.
        /// </summary>
        /// <param name="webResponse">The web response</param>
        /// <returns>True if the web response is a challenge for token authentication; otherwise, false</returns>
        protected internal virtual bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            return this.Credential.IsAuthenticationChallenge(webResponse);
        }

        /// <summary>
        /// Formats the authentication challenge string which this token provider handles.
        /// </summary>
        /// <returns>A string representing the handled authentication challenge</returns>
        internal string GetAuthenticationParameters()
        {
            if (string.IsNullOrEmpty(this.AuthenticationParameter))
            {
                return this.AuthenticationScheme;
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, this.AuthenticationScheme, this.AuthenticationParameter);
            }
        }

        /// <summary>
        /// Validates the current token if the provided reference is the current token and it 
        /// has not been validated before.
        /// </summary>
        /// <param name="token">The token which should be validated</param>
        /// <param name="webResponse">The web response which used the token</param>
        internal void ValidateToken(
            IssuedToken token,
            IHttpResponse webResponse)
        {
            if (token == null)
            {
                return;
            }

            lock (m_thisLock)
            {
                IssuedToken tokenToValidate = OnValidatingToken(token, webResponse);

                if (tokenToValidate.IsAuthenticated)
                {
                    return;
                }

                try
                {
                    // Perform validation which may include matching user information from the response
                    // with that from the stored connection. If user information mismatch, an exception
                    // will be thrown and the token will not be authenticated, which means if the same
                    // token is ever used again in a different request it will be revalidated and fail.
                    tokenToValidate.GetUserData(webResponse);
                    OnTokenValidated(tokenToValidate);

                    // Set the token to be authenticated.
                    tokenToValidate.Authenticated();
                }
                finally
                {
                    // When the token fails validation, we null its reference from the token provider so it
                    // would not be used again by the consumers of both. Note that we only update the current 
                    // token of the provider if it is the original token being validated, because we do not 
                    // want to overwrite a different token.
                    if (object.ReferenceEquals(this.CurrentToken, token))
                    {
                        this.CurrentToken = tokenToValidate.IsAuthenticated ? tokenToValidate : null;
                    }
                }
            }
        }

        /// <summary>
        /// Invalidates the current token if the provided reference is the current token.
        /// </summary>
        /// <param name="token">The token reference which should be invalidated</param>
        internal void InvalidateToken(IssuedToken token)
        {
            bool invalidated = false;
            lock (m_thisLock)
            {
                if (token != null && object.ReferenceEquals(this.CurrentToken, token))
                {
                    this.CurrentToken = null;
                    invalidated = true;
                }
            }

            if (invalidated)
            {
                OnTokenInvalidated(token);
            }
        }

        /// <summary>
        /// Retrieves a token for the credentials.
        /// </summary>
        /// <param name="failedToken">The token which previously failed authentication, if available</param>
        /// <param name="cancellationToken">The <c>CancellationToken</c>that will be assigned to the new task</param>
        /// <returns>A security token for the current credentials</returns>
        public async Task<IssuedToken> GetTokenAsync(
            IssuedToken failedToken,
            CancellationToken cancellationToken)
        {
            IssuedToken currentToken = this.CurrentToken;
            VssTraceActivity traceActivity = VssTraceActivity.Current;
            Stopwatch aadAuthTokenTimer = Stopwatch.StartNew();
            try
            {
                VssHttpEventSource.Log.AuthenticationStart(traceActivity);

                if (currentToken != null)
                {
                    VssHttpEventSource.Log.IssuedTokenRetrievedFromCache(traceActivity, this, currentToken);
                    return currentToken;
                }
                else
                {
                    GetTokenOperation operation = null;
                    try
                    {
                        GetTokenOperation operationInProgress;
                        operation = CreateOperation(traceActivity, failedToken, cancellationToken, out operationInProgress);
                        if (operationInProgress == null)
                        {
                            return await operation.GetTokenAsync(traceActivity).ConfigureAwait(false);
                        }
                        else
                        {
                            return await operationInProgress.WaitForTokenAsync(traceActivity, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        lock (m_thisLock)
                        {
                            m_operations.Remove(operation);
                        }

                        operation?.Dispose();
                    }
                }
            }
            finally
            {
                VssHttpEventSource.Log.AuthenticationStop(traceActivity);

                aadAuthTokenTimer.Stop();
                TimeSpan getTokenTime = aadAuthTokenTimer.Elapsed;

                if(getTokenTime.TotalSeconds >= c_slowTokenAcquisitionTimeInSeconds)
                {
                    // It may seem strange to pass the string value of TotalSeconds into this method, but testing
                    // showed that ETW is persnickety when you register a method in an EventSource that doesn't
                    // use strings or integers as its parameters. It is easier to simply give the method a string
                    // than figure out to get ETW to reliably accept a double or TimeSpan.
                    VssHttpEventSource.Log.AuthorizationDelayed(getTokenTime.TotalSeconds.ToString());
                }
            }
        }

        /// <summary>
        /// Retrieves a token for the credentials.
        /// </summary>
        /// <param name="failedToken">The token which previously failed authentication, if available</param>
        /// <param name="cancellationToken">The <c>CancellationToken</c>that will be assigned to the new task</param>
        /// <returns>A security token for the current credentials</returns>
        protected virtual Task<IssuedToken> OnGetTokenAsync(
            IssuedToken failedToken,
            CancellationToken cancellationToken)
        {
            if (this.Credential.Prompt != null)
            {
                return this.Credential.Prompt.GetTokenAsync(this, failedToken);
            }
            else
            {
                return Task.FromResult<IssuedToken>(null);
            }
        }

        /// <summary>
        /// Invoked when the current token is being validated. When overriden in a derived class,
        /// validate and return the validated token.
        /// </summary>
        /// <remarks>Is called inside a lock in <c>ValidateToken</c></remarks>
        /// <param name="token">The token to validate</param>
        /// <param name="webResponse">The web response which used the token</param>
        /// <returns>The validated token</returns>
        protected virtual IssuedToken OnValidatingToken(
            IssuedToken token,
            IHttpResponse webResponse)
        {
            return token;
        }

        protected virtual void OnTokenValidated(IssuedToken token)
        {
            // Store the validated token to the token storage if it is not originally from there.
            if (!token.FromStorage && TokenStorageUrl != null)
            {
                Credential.Storage?.StoreToken(TokenStorageUrl, token);
            }

            VssHttpEventSource.Log.IssuedTokenValidated(VssTraceActivity.Current, this, token);
        }

        protected virtual void OnTokenInvalidated(IssuedToken token)
        {
            if (Credential.Storage != null && TokenStorageUrl != null)
            {
                Credential.Storage.RemoveTokenValue(TokenStorageUrl, token);
            }

            VssHttpEventSource.Log.IssuedTokenInvalidated(VssTraceActivity.Current, this, token);
        }

        private GetTokenOperation CreateOperation(
            VssTraceActivity traceActivity,
            IssuedToken failedToken,
            CancellationToken cancellationToken,
            out GetTokenOperation operationInProgress)
        {
            operationInProgress = null;
            GetTokenOperation operation = null;
            lock (m_thisLock)
            {
                if (m_operations == null)
                {
                    m_operations = new List<GetTokenOperation>();
                }

                // Grab the main operation which is doing the work (if any)
                if (m_operations.Count > 0)
                {
                    operationInProgress = m_operations[0];

                    // Use the existing completion source when creating the new operation
                    operation = new GetTokenOperation(traceActivity, this, failedToken, cancellationToken, operationInProgress.CompletionSource);
                }
                else
                {
                    operation = new GetTokenOperation(traceActivity, this, failedToken, cancellationToken);
                }

                m_operations.Add(operation);
            }

            return operation;
        }

        private object m_thisLock;
        private List<GetTokenOperation> m_operations;

        private class DisposableTaskCompletionSource<T> : TaskCompletionSource<T>, IDisposable
        {
            public DisposableTaskCompletionSource()
            {
                this.Task.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { m_completed = true; });
            }

            ~DisposableTaskCompletionSource()
            {
                TraceErrorIfNotCompleted();
            }

            public void Dispose()
            {
                if (m_disposed)
                {
                    return;
                }

                TraceErrorIfNotCompleted();

                m_disposed = true;
                GC.SuppressFinalize(this);
            }

            private void TraceErrorIfNotCompleted()
            {
                if (!m_completed)
                {
                    VssHttpEventSource.Log.TokenSourceNotCompleted();
                }
            }

            private Boolean m_disposed;
            private Boolean m_completed;
        }

        private sealed class GetTokenOperation : IDisposable
        {
            public GetTokenOperation(
                VssTraceActivity activity,
                IssuedTokenProvider provider,
                IssuedToken failedToken,
                CancellationToken cancellationToken)
                : this(activity, provider, failedToken, cancellationToken, new DisposableTaskCompletionSource<IssuedToken>(), true)
            {
            }

            public GetTokenOperation(
                VssTraceActivity activity,
                IssuedTokenProvider provider,
                IssuedToken failedToken,
                CancellationToken cancellationToken,
                DisposableTaskCompletionSource<IssuedToken> completionSource,
                Boolean ownsCompletionSource = false)
            {
                this.Provider = provider;
                this.ActivityId = activity?.Id ?? Guid.Empty;
                this.FailedToken = failedToken;
                this.CancellationToken = cancellationToken;
                this.CompletionSource = completionSource;
                this.OwnsCompletionSource = ownsCompletionSource;
            }

            public Guid ActivityId { get; }

            public CancellationToken CancellationToken { get; }

            public DisposableTaskCompletionSource<IssuedToken> CompletionSource { get; }

            public Boolean OwnsCompletionSource { get; }

            private IssuedToken FailedToken { get; }

            private IssuedTokenProvider Provider { get; }

            public void Dispose()
            {
                if (this.OwnsCompletionSource)
                {
                    this.CompletionSource?.Dispose();
                }
            }

            public async Task<IssuedToken> GetTokenAsync(VssTraceActivity traceActivity)
            {
                IssuedToken token = null;
                try
                {
                    VssHttpEventSource.Log.IssuedTokenAcquiring(traceActivity, this.Provider);
                    if (this.Provider.InvokeRequired)
                    {
                        // Post to the UI thread using the scheduler. This may return a new task object which needs
                        // to be awaited, since once we get to the UI thread there may be nothing to do if someone else
                        // preempts us.

                        // The cancellation token source is used to handle race conditions between scheduling and 
                        // waiting for the UI task to begin execution. The callback is responsible for disposing of
                        // the token source, since the thought here is that the callback will run eventually as the
                        // typical reason for not starting execution within the timeout is due to a deadlock with
                        // the scheduler being used.
                        var timerTask = new TaskCompletionSource<Object>();
                        var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        timeoutTokenSource.Token.Register(() => timerTask.SetResult(null), false);

                        var uiTask = Task.Factory.StartNew((state) => PostCallback(state, timeoutTokenSource),
                                                            this,
                                                            this.CancellationToken,
                                                            TaskCreationOptions.None,
                                                            this.Provider.Credential.Scheduler).Unwrap();

                        var completedTask = await Task.WhenAny(timerTask.Task, uiTask).ConfigureAwait(false);
                        if (completedTask == uiTask)
                        {
                            token = uiTask.Result;
                        }
                    }
                    else
                    {
                        token = await this.Provider.OnGetTokenAsync(this.FailedToken, this.CancellationToken).ConfigureAwait(false);
                    }

                    CompletionSource.TrySetResult(token);
                    return token;
                }
                catch (Exception exception)
                {
                    // Mark our completion source as failed so other waiters will get notified in all cases
                    CompletionSource.TrySetException(exception);
                    throw;
                }
                finally
                {
                    this.Provider.CurrentToken = token ?? this.FailedToken;
                    VssHttpEventSource.Log.IssuedTokenAcquired(traceActivity, this.Provider, token);
                }
            }

            public async Task<IssuedToken> WaitForTokenAsync(
                VssTraceActivity traceActivity,
                CancellationToken cancellationToken)
            {
                IssuedToken token = null;
                try
                {

                    VssHttpEventSource.Log.IssuedTokenWaitStart(traceActivity, this.Provider, this.ActivityId);
                token = await Task.Factory.ContinueWhenAll<IssuedToken>(new Task[] { CompletionSource.Task }, (x) => CompletionSource.Task.Result, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    VssHttpEventSource.Log.IssuedTokenWaitStop(traceActivity, this.Provider, token);
                }

                return token;
            }

            private static Task<IssuedToken> PostCallback(
                Object state,
                CancellationTokenSource timeoutTokenSource)
            {
                // Make sure that we were not cancelled (timed out) before this callback is invoked.
                using (timeoutTokenSource)
                {
                    timeoutTokenSource.CancelAfter(-1);
                    if (timeoutTokenSource.IsCancellationRequested)
                    {
                        return Task.FromResult<IssuedToken>(null);
                    }
                }

                GetTokenOperation thisPtr = (GetTokenOperation)state;
                return thisPtr.Provider.OnGetTokenAsync(thisPtr.FailedToken, thisPtr.CancellationToken);
            }
        }
    }
}
