using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.Common.Internal;
using GitHub.Services.OAuth;

namespace GitHub.Services.Common
{
    public class RawHttpMessageHandler: HttpMessageHandler
    {
        public RawHttpMessageHandler(
            FederatedCredential credentials)
            : this(credentials, new RawClientHttpRequestSettings())
        {
        }

        public RawHttpMessageHandler(
            FederatedCredential credentials,
            RawClientHttpRequestSettings settings)
            : this(credentials, settings, new HttpClientHandler())
        {
        }

        public RawHttpMessageHandler(
            FederatedCredential credentials,
            RawClientHttpRequestSettings settings,
            HttpMessageHandler innerHandler)
        {
            this.Credentials = credentials;
            this.Settings = settings;
            m_messageInvoker = new HttpMessageInvoker(innerHandler);
            m_credentialWrapper = new CredentialWrapper();

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

            m_thisLock = new Object();
        }

        /// <summary>
        /// Gets the credentials associated with this handler.
        /// </summary>
        public FederatedCredential Credentials
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the settings associated with this handler.
        /// </summary>
        public RawClientHttpRequestSettings Settings
        {
            get;
            private set;
        }

        // setting this to WebRequest.DefaultWebProxy in NETSTANDARD is causing a System.PlatformNotSupportedException
        //.in System.Net.SystemWebProxy.IsBypassed.  Comment in IsBypassed method indicates ".NET Core and .NET Native
        // code will handle this exception and call into WinInet/WinHttp as appropriate to use the system proxy."
        // This needs to be investigated further.
        private static IWebProxy s_defaultWebProxy = null;

        /// <summary>
        /// Allows you to set a proxy to be used by all RawHttpMessageHandler requests without affecting the global WebRequest.DefaultWebProxy.  If not set it returns the WebRequest.DefaultWebProxy.
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

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            VssTraceActivity traceActivity = VssTraceActivity.Current;

            lock (m_thisLock)
            {
                // Ensure that we attempt to use the most appropriate authentication mechanism by default.
                if (m_tokenProvider == null)
                {
                    m_tokenProvider = this.Credentials.CreateTokenProvider(request.RequestUri, null, null);
                }
            }

            CancellationTokenSource tokenSource = null;
            HttpResponseMessage response = null;
            Boolean succeeded = false;
            HttpResponseMessageWrapper responseWrapper;

            Int32 retries = m_maxAuthRetries;
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

                    // Let's start with sending a token
                    IssuedToken token = await m_tokenProvider.GetTokenAsync(null, tokenSource.Token).ConfigureAwait(false);
                    ApplyToken(request, token);

                    // ConfigureAwait(false) enables the continuation to be run outside any captured
                    // SyncronizationContext (such as ASP.NET's) which keeps things from deadlocking...
                    response = await m_messageInvoker.SendAsync(request, tokenSource.Token).ConfigureAwait(false);

                    responseWrapper = new HttpResponseMessageWrapper(response);

                    var isUnAuthorized = responseWrapper.StatusCode == HttpStatusCode.Unauthorized;
                    if (!isUnAuthorized)
                    {
                        // Validate the token after it has been successfully authenticated with the server.
                        m_tokenProvider?.ValidateToken(token, responseWrapper);
                        succeeded = true;
                        break;
                    }
                    else
                    {
                        m_tokenProvider?.InvalidateToken(token);

                        if (retries == 0 || retries < m_maxAuthRetries)
                        {
                            break;
                        }

                        token = await m_tokenProvider.GetTokenAsync(token, tokenSource.Token).ConfigureAwait(false);

                        retries--;
                    }
                }
                while (retries >= 0);

                // We're out of retries and the response was an auth challenge -- then the request was unauthorized
                // and we will throw a strongly-typed exception with a friendly error message.
                if (!succeeded && response != null && responseWrapper.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Make sure we do not leak the response object when raising an exception
                    if (response != null)
                    {
                        response.Dispose();
                    }

                    var message = CommonResources.VssUnauthorized(request.RequestUri.GetLeftPart(UriPartial.Authority));
                    VssHttpEventSource.Log.HttpRequestUnauthorized(traceActivity, request, message);
                    VssUnauthorizedException unauthorizedException = new VssUnauthorizedException(message);
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
            }
        }

        private void ApplyToken(
            HttpRequestMessage request,
            IssuedToken token)
        {
            switch (token)
            {
                case null:
                    return;
                case ICredentials credentialsToken:
                    m_credentialWrapper.InnerCredentials = credentialsToken;
                    break;
                default:
                    token.ApplyTo(new HttpRequestMessageWrapper(request));
                    break;
            }
        }

        private static void ApplySettings(
            HttpMessageHandler handler,
            ICredentials defaultCredentials,
            RawClientHttpRequestSettings settings)
        {
            HttpClientHandler httpClientHandler = handler as HttpClientHandler;
            if (httpClientHandler != null)
            {
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
            }
        }

        private readonly HttpMessageHandler m_transportHandler;
        private HttpMessageInvoker m_messageInvoker;
        private CredentialWrapper m_credentialWrapper;
        private object m_thisLock;
        private const Int32 m_maxAuthRetries = 3;
        private IssuedTokenProvider m_tokenProvider;

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
