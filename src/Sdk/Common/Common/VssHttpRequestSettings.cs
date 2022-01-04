using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides common settings for a <c>VssHttpMessageHandler</c> instance.
    /// </summary>
    public class VssHttpRequestSettings
    {
        /// <summary>
        /// Initializes a new <c>VssHttpRequestSettings</c> instance with compression enabled.
        /// </summary>
        public VssHttpRequestSettings()
            : this(Guid.NewGuid())
        {
        }

        /// <summary>
        /// Initializes a new <c>VssHttpRequestSettings</c> instance with compression enabled.
        /// </summary>
        public VssHttpRequestSettings(Guid sessionId)
        {
            this.AllowAutoRedirect = false;
            this.CompressionEnabled = true;
            this.ExpectContinue = true;
            this.BypassProxyOnLocal = true;
            this.MaxContentBufferSize = c_defaultContentBufferSize;
            this.SendTimeout = s_defaultTimeout;
            if (!String.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name)) // InvariantCulture for example has an empty name.
            {
                this.AcceptLanguages.Add(CultureInfo.CurrentUICulture);
            }
            this.SessionId = sessionId;
            this.SuppressFedAuthRedirects = true;
            this.ClientCertificateManager = null;
            this.ServerCertificateValidationCallback = null;
            this.UseHttp11 = false;

            // If different, we'll also add CurrentCulture to the request headers,
            // but UICulture was added first, so it gets first preference
            if (!CultureInfo.CurrentCulture.Equals(CultureInfo.CurrentUICulture) && !String.IsNullOrEmpty(CultureInfo.CurrentCulture.Name))
            {
                this.AcceptLanguages.Add(CultureInfo.CurrentCulture);
            }

            this.MaxRetryRequest = c_defaultMaxRetry;

#if DEBUG
            string customClientRequestTimeout = Environment.GetEnvironmentVariable("VSS_Client_Request_Timeout");
            if (!string.IsNullOrEmpty(customClientRequestTimeout) && int.TryParse(customClientRequestTimeout, out int customTimeout))
            {
                // avoid disrupting a debug session due to the request timing out by setting a custom timeout.
                this.SendTimeout = TimeSpan.FromSeconds(customTimeout);
            }
#endif
        }

        /// <summary>
        /// Initializes a new <c>VssHttpRequestSettings</c> instance with compression enabled.
        /// </summary>
        /// <remarks>The e2eId argument is not used.</remarks>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public VssHttpRequestSettings(Guid sessionId, Guid e2eId)
            : this(sessionId)
        {
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="copy"></param>
        protected VssHttpRequestSettings(VssHttpRequestSettings copy)
        {
            this.AllowAutoRedirect = copy.AllowAutoRedirect;
            this.CompressionEnabled = copy.CompressionEnabled;
            this.ExpectContinue = copy.ExpectContinue;
            this.BypassProxyOnLocal = copy.BypassProxyOnLocal;
            this.MaxContentBufferSize = copy.MaxContentBufferSize;
            this.SendTimeout = copy.SendTimeout;
            this.m_acceptLanguages = new List<CultureInfo>(copy.AcceptLanguages);
            this.SessionId = copy.SessionId;
            this.AgentId = copy.AgentId;
            this.SuppressFedAuthRedirects = copy.SuppressFedAuthRedirects;
            this.UserAgent = new List<ProductInfoHeaderValue>(copy.UserAgent);
            this.OperationName = copy.OperationName;
            this.ClientCertificateManager = copy.ClientCertificateManager;
            this.ServerCertificateValidationCallback = copy.ServerCertificateValidationCallback;
            this.MaxRetryRequest = copy.MaxRetryRequest;
            this.UseHttp11 = copy.UseHttp11;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not HttpClientHandler should follow redirect on outgoing requests. 
        /// </summary>
        public Boolean AllowAutoRedirect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not compression should be used on outgoing requests. 
        /// The default value is true.
        /// </summary>
        [DefaultValue(true)]
        public Boolean CompressionEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the Expect: 100-continue header should be sent on
        /// outgoing requess. The default value is true.
        /// </summary>
        [DefaultValue(true)]
        public Boolean ExpectContinue
        {
            get;
            set;
        }

        /// <summary>
        /// Sets whether to bypass web proxies if the call is local
        /// </summary>
        public Boolean BypassProxyOnLocal
        {
            get;
            set;
        }

        /// <summary>
        /// The .NET Core 2.1 runtime switched its HTTP default from HTTP 1.1 to HTTP 2.
        /// This causes problems with some versions of the Curl handler on Linux.
        /// See GitHub issue https://github.com/dotnet/corefx/issues/32376
        /// If true, requests generated by this client will use HTTP 1.1.
        /// </summary>
        public Boolean UseHttp11
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum size allowed for response content buffering.
        /// </summary>
        [DefaultValue(c_defaultContentBufferSize)]
        public Int32 MaxContentBufferSize
        {
            get
            {
                return m_maxContentBufferSize;
            }
            set
            {
                ArgumentUtility.CheckForOutOfRange(value, nameof(value), 0, c_maxAllowedContentBufferSize);
                m_maxContentBufferSize = value;
            }
        }

        /// <summary>
        /// Timespan to wait before timing out a request. Defaults to 100 seconds
        /// </summary>
        public TimeSpan SendTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Provides a hint to the server requesting that rather than getting 302 redirects as part of FedAuth flows 401 and 403 are passed through.
        /// </summary>
        [DefaultValue(true)]
        public Boolean SuppressFedAuthRedirects
        {
            get;
            set;
        }

        /// <summary>
        /// User-Agent header passed along in the request,
        /// For multiple values, the order in the list is the order
        /// in which they will appear in the header
        /// </summary>
        public List<ProductInfoHeaderValue> UserAgent
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the culture is passed in the Accept-Language header
        /// </summary>
        public ICollection<CultureInfo> AcceptLanguages
        {
            get
            {
                return m_acceptLanguages;
            }
        }

        /// <summary>
        /// A unique identifier for the user session
        /// </summary>
        public Guid SessionId
        {
            get;
            set;
        }

        /// <summary>
        /// End to End ID which gets propagated everywhere unchanged
        /// </summary>
        public Guid E2EId
        {
            get;
            set;
        }

        /// <summary>
        /// This is a kind of combination between SessionId and UserAgent.
        /// If supplied, the value should be a string that uniquely identifies
        /// this application running on this particular machine.
        /// The server will then use this value
        /// to correlate user requests, even if the process restarts.
        /// </summary>
        public String AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// An optional string that is sent in the SessionId header used to group a set of operations together
        /// </summary>
        public String OperationName
        {
            get;
            set;
        }

        /// <summary>
        /// Optional implementation used to gather client certificates
        /// for connections that require them
        /// </summary>
        public IVssClientCertificateManager ClientCertificateManager
        {
            get;
            set;
        }

        /// <summary>
        /// Optional implementation used to validate server certificate validation
        /// </summary>
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateValidationCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Number of times to retry a request that has an ambient failure
        /// </summary>
        /// <remarks>
        /// This property is only used by VssConnection, so only relevant on the client
        /// </remarks>
        [DefaultValue(c_defaultMaxRetry)]
        public Int32 MaxRetryRequest
        {
            get;
            set;
        }

        protected internal virtual Boolean IsHostLocal(String hostName)
        {
            //base class always returns false. See VssClientHttpRequestSettings for override
            return false;
        }

        protected internal virtual Boolean ApplyTo(HttpRequestMessage request)
        {
            // Make sure we only apply the settings to the request once
            if (request.Properties.ContainsKey(PropertyName))
            {
                return false;
            }

            request.Properties.Add(PropertyName, this);

            if (this.AcceptLanguages != null && this.AcceptLanguages.Count > 0)
            {
                // An empty or null CultureInfo name will cause an ArgumentNullException in the
                // StringWithQualityHeaderValue constructor. CultureInfo.InvariantCulture is an example of
                // a CultureInfo that has an empty name.
                foreach (CultureInfo culture in this.AcceptLanguages.Where(a => !String.IsNullOrEmpty(a.Name)))
                {
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
                }
            }

            if (this.UserAgent != null)
            {
                foreach (var headerVal in this.UserAgent)
                {
                    if (!request.Headers.UserAgent.Contains(headerVal))
                    {
                        request.Headers.UserAgent.Add(headerVal);
                    }
                }
            }

            if (this.SuppressFedAuthRedirects)
            {
                request.Headers.Add(Internal.HttpHeaders.TfsFedAuthRedirect, "Suppress");
            }

            // Record the command, if we have it.  Otherwise, just record the session ID.
            if (!request.Headers.Contains(Internal.HttpHeaders.TfsSessionHeader))
            {
                if (!String.IsNullOrEmpty(this.OperationName))
                {
                    request.Headers.Add(Internal.HttpHeaders.TfsSessionHeader, String.Concat(this.SessionId.ToString("D"), ", ", this.OperationName));
                }
                else
                {
                    request.Headers.Add(Internal.HttpHeaders.TfsSessionHeader, this.SessionId.ToString("D"));
                }
            }

            if (!String.IsNullOrEmpty(this.AgentId))
            {
                request.Headers.Add(Internal.HttpHeaders.VssAgentHeader, this.AgentId);
            }

            // Content is being sent as chunked by default in dotnet5.4, which differs than the .net 4.5 behaviour.
            if (request.Content != null && !request.Content.Headers.ContentLength.HasValue && !request.Headers.TransferEncodingChunked.HasValue)
            {
                request.Content.Headers.ContentLength = request.Content.ReadAsByteArrayAsync().Result.Length;
            }

            return true;
        }

        /// <summary>
        /// Gets the encoding used for outgoing requests.
        /// </summary>
        public static Encoding Encoding
        {
            get
            {
                return s_encoding.Value;
            }
        }

        /// <summary>
        /// Gets the property name used to reference this object.
        /// </summary>
        public const String PropertyName = "MS.VS.RequestSettings";

        /// <summary>
        /// Gets the property name used to reference the completion option for a specific request.
        /// </summary>
        public const String HttpCompletionOptionPropertyName = "MS.VS.HttpCompletionOption";

        /// <summary>
        /// Header to include the light weight response client option.
        /// </summary>
        public const string LightweightHeader = "lightweight";

        /// <summary>
        /// Header to include the exclude urls client option.
        /// </summary>
        public const string ExcludeUrlsHeader = "excludeUrls";

        private Int32 m_maxContentBufferSize;
        private ICollection<CultureInfo> m_acceptLanguages = new List<CultureInfo>();
        private static Lazy<Encoding> s_encoding = new Lazy<Encoding>(() => new UTF8Encoding(false), LazyThreadSafetyMode.PublicationOnly);
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100); //default WebAPI timeout
        private const Int32 c_defaultMaxRetry = 3;

        // We will buffer a maximum of 1024MB in the message handler
        private const Int32 c_maxAllowedContentBufferSize = 1024 * 1024 * 1024;

        // We will buffer, by default, up to 512MB in the message handler
        private const Int32 c_defaultContentBufferSize = 1024 * 1024 * 512;
    }
}
