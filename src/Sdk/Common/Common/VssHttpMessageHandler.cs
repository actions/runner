using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides authentication for Visual Studio Services.
    /// </summary>
    public class VssHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// Initializes a new <c>VssHttpMessageHandler</c> instance with default credentials and request 
        /// settings.
        /// </summary>
        public VssHttpMessageHandler()
            : this(new VssCredentials(), new VssHttpRequestSettings())
        {
        }

        /// <summary>
        /// Initializes a new <c>VssHttpMessageHandler</c> instance with the specified credentials and request 
        /// settings.
        /// </summary>
        /// <param name="credentials">The credentials which should be used</param>
        /// <param name="settings">The request settings which should be used</param>
        public VssHttpMessageHandler(
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : this(credentials, settings, new HttpClientHandler())
        {
        }

        /// <summary>
        /// Initializes a new <c>VssHttpMessageHandler</c> instance with the specified credentials and request 
        /// settings.
        /// </summary>
        /// <param name="credentials">The credentials which should be used</param>
        /// <param name="settings">The request settings which should be used</param>
        /// <param name="innerHandler"></param>
        public VssHttpMessageHandler(
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            HttpMessageHandler innerHandler)
        {
            this.Credentials = credentials;
            this.Settings = settings;
            this.ExpectContinue = settings.ExpectContinue;

            m_credentialWrapper = new CredentialWrapper();
            m_messageInvoker = new HttpMessageInvoker(innerHandler);

            // If we were given a pipeline make sure we find the inner-most handler to apply our settings as this
            // will be the actual outgoing transport.
            {
                HttpMessageHandler transportHandler = innerHandler;
                DelegatingHandler delegatingHandler = transportHandler as DelegatingHandler;
                while (delegatingHandler != null)
                {
                    transportHandler = delegatingHandler.InnerHandler;
                    delegatingHandler = transportHandler as DelegatingHandler;
                }

                m_transportHandler = transportHandler;
            }

            ApplySettings(m_transportHandler, m_credentialWrapper, this.Settings);
        }

        /// <summary>
        /// Gets the credentials associated with this handler.
        /// </summary>
        public VssCredentials Credentials
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the settings associated with this handler.
        /// </summary>
        public VssHttpRequestSettings Settings
        {
            get;
            private set;
        }

        private Boolean ExpectContinue
        {
            get;
            set;
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (m_messageInvoker != null)
                {
                    m_messageInvoker.Dispose();
                }
            }
        }

        internal static readonly String PropertyName = "MS.VS.MessageHandler";

        /// <summary>
        /// Handles the authentication hand-shake for a Visual Studio service.
        /// </summary>
        /// <param name="request">The HTTP request message</param>
        /// <param name="cancellationToken">The cancellation token used for cooperative cancellation</param>
        /// <returns>A new <c>Task&lt;HttpResponseMessage&gt;</c> which wraps the response from the remote service</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            VssTraceActivity traceActivity = VssTraceActivity.Current;

            var traceInfo = VssHttpMessageHandlerTraceInfo.GetTraceInfo(request);
            traceInfo?.TraceHandlerStartTime();

            if (!m_appliedClientCertificatesToTransportHandler &&
                request.RequestUri.Scheme == "https")
            {
                HttpClientHandler httpClientHandler = m_transportHandler as HttpClientHandler;
                if (httpClientHandler != null &&
                    this.Settings.ClientCertificateManager != null &&
                    this.Settings.ClientCertificateManager.ClientCertificates != null &&
                    this.Settings.ClientCertificateManager.ClientCertificates.Count > 0)
                {
                    httpClientHandler.ClientCertificates.AddRange(this.Settings.ClientCertificateManager.ClientCertificates);
                }
                m_appliedClientCertificatesToTransportHandler = true;
            }

            if (!m_appliedServerCertificateValidationCallbackToTransportHandler &&
                request.RequestUri.Scheme == "https")
            {
                HttpClientHandler httpClientHandler = m_transportHandler as HttpClientHandler;
                if (httpClientHandler != null &&
                    this.Settings.ServerCertificateValidationCallback != null)
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = this.Settings.ServerCertificateValidationCallback;
                }
                m_appliedServerCertificateValidationCallbackToTransportHandler = true;
            }

            // The .NET Core 2.1 runtime switched its HTTP default from HTTP 1.1 to HTTP 2.
            // This causes problems with some versions of the Curl handler on Linux.
            // See GitHub issue https://github.com/dotnet/corefx/issues/32376
            if (Settings.UseHttp11)
            {
                request.Version = HttpVersion.Version11;
            }

            IssuedToken token = null;
            IssuedTokenProvider provider;
            if (this.Credentials.TryGetTokenProvider(request.RequestUri, out provider))
            {
                token = provider.CurrentToken;
            }

            // Add ourselves to the message so the underlying token issuers may use it if necessary
            request.Properties[VssHttpMessageHandler.PropertyName] = this;

            Boolean succeeded = false;
            Boolean lastResponseDemandedProxyAuth = false;
            Int32 retries = m_maxAuthRetries;
            HttpResponseMessage response = null;
            HttpResponseMessageWrapper responseWrapper;
            CancellationTokenSource tokenSource = null;

            try
            {
                tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                if (this.Settings.SendTimeout > TimeSpan.Zero)
                {
                    tokenSource.CancelAfter(this.Settings.SendTimeout);
                }

                do
                {
                    if (response != null)
                    {
                        response.Dispose();
                    }

                    ApplyHeaders(request);

                    // In the case of a Windows token, only apply it to the web proxy if it
                    // returned a 407 Proxy Authentication Required. If we didn't get this
                    // status code back, then the proxy (if there is one) is clearly working fine,
                    // so we shouldn't mess with its credentials.
                    ApplyToken(request, token, applyICredentialsToWebProxy: lastResponseDemandedProxyAuth);
                    lastResponseDemandedProxyAuth = false;

                    // The WinHttpHandler will chunk any content that does not have a computed length which is
                    // not what we want. By loading into a buffer up-front we bypass this behavior and there is
                    // no difference in the normal HttpClientHandler behavior here since this is what they were
                    // already doing.
                    await BufferRequestContentAsync(request, tokenSource.Token).ConfigureAwait(false);

                    traceInfo?.TraceBufferedRequestTime();

                    // ConfigureAwait(false) enables the continuation to be run outside any captured 
                    // SyncronizationContext (such as ASP.NET's) which keeps things from deadlocking...
                    response = await m_messageInvoker.SendAsync(request, tokenSource.Token).ConfigureAwait(false);

                    traceInfo?.TraceRequestSendTime();

                    // Now buffer the response content if configured to do so. In general we will be buffering
                    // the response content in this location, except in the few cases where the caller has 
                    // specified HttpCompletionOption.ResponseHeadersRead.
                    // Trace content type in case of error
                    await BufferResponseContentAsync(request, response, () => $"[ContentType: {response.Content.GetType().Name}]", tokenSource.Token).ConfigureAwait(false);

                    traceInfo?.TraceResponseContentTime();

                    responseWrapper = new HttpResponseMessageWrapper(response);

                    if (!this.Credentials.IsAuthenticationChallenge(responseWrapper))
                    {
                        // Validate the token after it has been successfully authenticated with the server.
                        if (provider != null)
                        {
                            provider.ValidateToken(token, responseWrapper);
                        }

                        // Make sure that once we can authenticate with the service that we turn off the 
                        // Expect100Continue behavior to increase performance.
                        this.ExpectContinue = false;
                        succeeded = true;
                        break;
                    }
                    else
                    {
                        // In the case of a Windows token, only apply it to the web proxy if it
                        // returned a 407 Proxy Authentication Required. If we didn't get this
                        // status code back, then the proxy (if there is one) is clearly working fine,
                        // so we shouldn't mess with its credentials.
                        lastResponseDemandedProxyAuth = responseWrapper.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;

                        // Invalidate the token and ensure that we have the correct token provider for the challenge
                        // which we just received
                        VssHttpEventSource.Log.AuthenticationFailed(traceActivity, response);

                        if (provider != null)
                        {
                            provider.InvalidateToken(token);
                        }

                        // Ensure we have an appropriate token provider for the current challenge
                        provider = this.Credentials.CreateTokenProvider(request.RequestUri, responseWrapper, token);

                        // Make sure we don't invoke the provider in an invalid state
                        if (provider == null)
                        {
                            VssHttpEventSource.Log.IssuedTokenProviderNotFound(traceActivity);
                            break;
                        }
                        else if (provider.GetTokenIsInteractive && this.Credentials.PromptType == CredentialPromptType.DoNotPrompt)
                        {
                            VssHttpEventSource.Log.IssuedTokenProviderPromptRequired(traceActivity, provider);
                            break;
                        }

                        // If the user has already tried once but still unauthorized, stop retrying. The main scenario for this condition
                        // is a user typed in a valid credentials for a hosted account but the associated identity does not have 
                        // access. We do not want to continually prompt 3 times without telling them the failure reason. In the 
                        // next release we should rethink about presenting user the failure and options between retries.
                        IEnumerable<String> headerValues;
                        Boolean hasAuthenticateError =
                            response.Headers.TryGetValues(HttpHeaders.VssAuthenticateError, out headerValues) &&
                            !String.IsNullOrEmpty(headerValues.FirstOrDefault());

                        if (retries == 0 || (retries < m_maxAuthRetries && hasAuthenticateError))
                        {
                            break;
                        }

                        // Now invoke the provider and await the result
                        token = await provider.GetTokenAsync(token, tokenSource.Token).ConfigureAwait(false);

                        // I always see 0 here, but the method above could take more time so keep for now
                        traceInfo?.TraceGetTokenTime();

                        // If we just received a token, lets ask the server for the VSID
                        request.Headers.Add(HttpHeaders.VssUserData, String.Empty);

                        retries--;
                    }
                }
                while (retries >= 0);

                if (traceInfo != null)
                {
                    traceInfo.TokenRetries = m_maxAuthRetries - retries;
                }

                // We're out of retries and the response was an auth challenge -- then the request was unauthorized
                // and we will throw a strongly-typed exception with a friendly error message.
                if (!succeeded && response != null && this.Credentials.IsAuthenticationChallenge(responseWrapper))
                {
                    String message = null;
                    IEnumerable<String> serviceError;

                    if (response.Headers.TryGetValues(HttpHeaders.TfsServiceError, out serviceError))
                    {
                        message = UriUtility.UrlDecode(serviceError.FirstOrDefault());
                    }
                    else
                    {
                        message = CommonResources.VssUnauthorized(request.RequestUri.GetLeftPart(UriPartial.Authority));
                    }

                    // Make sure we do not leak the response object when raising an exception
                    if (response != null)
                    {
                        response.Dispose();
                    }

                    VssHttpEventSource.Log.HttpRequestUnauthorized(traceActivity, request, message);
                    VssUnauthorizedException unauthorizedException = new VssUnauthorizedException(message);

                    if (provider != null)
                    {
                        unauthorizedException.Data.Add(CredentialsType, provider.CredentialType);
                    }

                    throw unauthorizedException;
                }

                return response;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    VssHttpEventSource.Log.HttpRequestCancelled(traceActivity, request);
                    throw;
                }
                else
                {
                    VssHttpEventSource.Log.HttpRequestTimedOut(traceActivity, request, this.Settings.SendTimeout);
                    throw new TimeoutException(CommonResources.HttpRequestTimeout(this.Settings.SendTimeout), ex);
                }
            }
            finally
            {
                // We always dispose of the token source since otherwise we leak resources if there is a timer pending
                if (tokenSource != null)
                {
                    tokenSource.Dispose();
                }

                traceInfo?.TraceTrailingTime();
            }
        }

        private static async Task BufferRequestContentAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content != null &&
                request.Headers.TransferEncodingChunked != true)
            {
                Int64? contentLength = request.Content.Headers.ContentLength;
                if (contentLength == null)
                {
                    await request.Content.LoadIntoBufferAsync().EnforceCancellation(cancellationToken).ConfigureAwait(false);
                }

                // Explicitly turn off chunked encoding since we have computed the request content size
                request.Headers.TransferEncodingChunked = false;
            }
        }

        protected virtual async Task BufferResponseContentAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            Func<string> makeErrorMessage,
            CancellationToken cancellationToken)
        {
            // Determine whether or not we should go ahead and buffer the output under our timeout scope. If
            // we do not perform this action here there is a potential network stack hang since we override
            // the HttpClient.SendTimeout value and the cancellation token for monitoring request timeout does
            // not survive beyond this scope.
            if (response == null || response.StatusCode == HttpStatusCode.NoContent || response.Content == null)
            {
                return;
            }

            // Do not try to buffer with a size of 0. This forces all calls to effectively use the behavior of
            // HttpCompletionOption.ResponseHeadersRead if that is desired.
            if (this.Settings.MaxContentBufferSize == 0)
            {
                return;
            }

            // Read the completion option provided by the caller. If we don't find the property then we
            // assume it is OK to buffer by default.
            HttpCompletionOption completionOption;
            if (!request.Properties.TryGetValue(VssHttpRequestSettings.HttpCompletionOptionPropertyName, out completionOption))
            {
                completionOption = HttpCompletionOption.ResponseContentRead;
            }

            // If the caller specified that response content should be read then we need to go ahead and
            // buffer it all up to the maximum buffer size specified by the settings. Anything larger than
            // the maximum will trigger an error in the underlying stack.
            if (completionOption == HttpCompletionOption.ResponseContentRead)
            {
                await response.Content.LoadIntoBufferAsync(this.Settings.MaxContentBufferSize).EnforceCancellation(cancellationToken, makeErrorMessage).ConfigureAwait(false);
            }
        }

        private void ApplyHeaders(HttpRequestMessage request)
        {
            if (this.Settings.ApplyTo(request))
            {
                VssTraceActivity activity = request.GetActivity();
                if (activity != null &&
                    activity != VssTraceActivity.Empty &&
                    !request.Headers.Contains(HttpHeaders.TfsSessionHeader))
                {
                    request.Headers.Add(HttpHeaders.TfsSessionHeader, activity.Id.ToString("D"));
                }

                request.Headers.ExpectContinue = this.ExpectContinue;
            }
        }

        private void ApplyToken(
            HttpRequestMessage request,
            IssuedToken token,
            bool applyICredentialsToWebProxy = false)
        {
            if (token == null)
            {
                return;
            }

            ICredentials credentialsToken = token as ICredentials;
            if (credentialsToken != null)
            {
                if (applyICredentialsToWebProxy)
                {
                    HttpClientHandler httpClientHandler = m_transportHandler as HttpClientHandler;

                    if (httpClientHandler != null &&
                        httpClientHandler.Proxy != null)
                    {
                        httpClientHandler.Proxy.Credentials = credentialsToken;
                    }
                }

                m_credentialWrapper.InnerCredentials = credentialsToken;
            }
            else
            {
                token.ApplyTo(new HttpRequestMessageWrapper(request));
            }
        }

        private static void ApplySettings(
            HttpMessageHandler handler,
            ICredentials defaultCredentials,
            VssHttpRequestSettings settings)
        {
            HttpClientHandler httpClientHandler = handler as HttpClientHandler;
            if (httpClientHandler != null)
            {
                httpClientHandler.AllowAutoRedirect = settings.AllowAutoRedirect;
                httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                //Setting httpClientHandler.UseDefaultCredentials to false in .Net Core, clears httpClientHandler.Credentials if
                //credentials is already set to defaultcredentials. Therefore httpClientHandler.Credentials must be 
                //set after httpClientHandler.UseDefaultCredentials.
                httpClientHandler.UseDefaultCredentials = false;
                httpClientHandler.Credentials = defaultCredentials;
                httpClientHandler.PreAuthenticate = false;
                httpClientHandler.Proxy = DefaultWebProxy;
                httpClientHandler.UseCookies = false;
                httpClientHandler.UseProxy = true;

                if (settings.CompressionEnabled)
                {
                    httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip;
                }
            }
        }

        // setting this to WebRequest.DefaultWebProxy in NETSTANDARD is causing a System.PlatformNotSupportedException
        //.in System.Net.SystemWebProxy.IsBypassed.  Comment in IsBypassed method indicates ".NET Core and .NET Native
        // code will handle this exception and call into WinInet/WinHttp as appropriate to use the system proxy."
        // This needs to be investigated further.
        private static IWebProxy s_defaultWebProxy = null;

        /// <summary>
        /// Allows you to set a proxy to be used by all VssHttpMessageHandler requests without affecting the global WebRequest.DefaultWebProxy.  If not set it returns the WebRequest.DefaultWebProxy.
        /// </summary>
        public static IWebProxy DefaultWebProxy
        {
            get
            {
                var toReturn = WebProxyWrapper.Wrap(s_defaultWebProxy);

                if (null != toReturn &&
                    toReturn.Credentials == null)
                {
                    toReturn.Credentials = CredentialCache.DefaultCredentials;
                }

                return toReturn;
            }
            set
            {
                s_defaultWebProxy = value;
            }
        }

        internal const String CredentialsType = nameof(CredentialsType);

        private const Int32 m_maxAuthRetries = 3;
        private HttpMessageInvoker m_messageInvoker;
        private CredentialWrapper m_credentialWrapper;
        private bool m_appliedClientCertificatesToTransportHandler;
        private bool m_appliedServerCertificateValidationCallbackToTransportHandler;
        private readonly HttpMessageHandler m_transportHandler;

        //.Net Core does not attempt NTLM schema on Linux, unless ICredentials is a CredentialCache instance
        //This workaround may not be needed after this corefx fix is consumed: https://github.com/dotnet/corefx/pull/7923
        private sealed class CredentialWrapper : CredentialCache, ICredentials
        {
            public ICredentials InnerCredentials
            {
                get;
                set;
            }

            NetworkCredential ICredentials.GetCredential(
                Uri uri,
                String authType)
            {
                return InnerCredentials != null ? InnerCredentials.GetCredential(uri, authType) : null;
            }
        }

        private sealed class WebProxyWrapper : IWebProxy
        {
            private WebProxyWrapper(IWebProxy toWrap)
            {
                m_wrapped = toWrap;
                m_credentials = null;
            }

            public static WebProxyWrapper Wrap(IWebProxy toWrap)
            {
                if (null == toWrap)
                {
                    return null;
                }

                return new WebProxyWrapper(toWrap);
            }

            public ICredentials Credentials
            {
                get
                {
                    ICredentials credentials = m_credentials;

                    if (null == credentials)
                    {
                        // This means to fall back to the Credentials from the wrapped
                        // IWebProxy.
                        credentials = m_wrapped.Credentials;
                    }
                    else if (Object.ReferenceEquals(credentials, m_nullCredentials))
                    {
                        // This sentinel value means we have explicitly had our credentials
                        // set to null.
                        credentials = null;
                    }

                    return credentials;
                }

                set
                {
                    if (null == value)
                    {
                        // Use this as a sentinel value to distinguish the case when someone has
                        // explicitly set our credentials to null. We don't want to fall back to
                        // m_wrapped.Credentials when we have credentials that are explicitly null.
                        m_credentials = m_nullCredentials;
                    }
                    else
                    {
                        m_credentials = value;
                    }
                }
            }

            public Uri GetProxy(Uri destination)
            {
                return m_wrapped.GetProxy(destination);
            }

            public bool IsBypassed(Uri host)
            {
                return m_wrapped.IsBypassed(host);
            }

            private readonly IWebProxy m_wrapped;
            private ICredentials m_credentials;

            private static readonly ICredentials m_nullCredentials = new CredentialWrapper();
        }
    }
}
