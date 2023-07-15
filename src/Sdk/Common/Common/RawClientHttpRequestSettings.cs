using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using GitHub.Services.WebApi.Utilities.Internal;

namespace GitHub.Services.Common
{
    public class RawClientHttpRequestSettings
    {
        /// <summary>
        /// Timespan to wait before timing out a request. Defaults to 100 seconds
        /// </summary>
        public TimeSpan SendTimeout
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
        /// This property is only used by RawConnection, so only relevant on the client
        /// </remarks>
        [DefaultValue(c_defaultMaxRetry)]
        public Int32 MaxRetryRequest
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the property name used to reference this object.
        /// </summary>
        public const String PropertyName = "Actions.RequestSettings";

        public static RawClientHttpRequestSettings Default => s_defaultSettings.Value;

        protected RawClientHttpRequestSettings(RawClientHttpRequestSettings copy)
        {
            this.SendTimeout = copy.SendTimeout;
            this.m_acceptLanguages = new List<CultureInfo>(copy.AcceptLanguages);
            this.SessionId = copy.SessionId;
            this.UserAgent = new List<ProductInfoHeaderValue>(copy.UserAgent);
            this.ServerCertificateValidationCallback = copy.ServerCertificateValidationCallback;
            this.MaxRetryRequest = copy.MaxRetryRequest;
        }

        public RawClientHttpRequestSettings Clone()
        {
            return new RawClientHttpRequestSettings(this);
        }

        public RawClientHttpRequestSettings()
            : this(Guid.NewGuid())
        {
        }

        public RawClientHttpRequestSettings(Guid sessionId)
        {
            this.SendTimeout = s_defaultTimeout;
            if (!String.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name)) // InvariantCulture for example has an empty name.
            {
                this.AcceptLanguages.Add(CultureInfo.CurrentUICulture);
            }
            this.SessionId = sessionId;
            this.ServerCertificateValidationCallback = null;

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

        protected internal virtual Boolean ApplyTo(HttpRequestMessage request)
        {
            // Make sure we only apply the settings to the request once
            if (request.Options.TryGetValue<object>(PropertyName, out _))
            {
                return false;
            }

            request.Options.Set(new HttpRequestOptionsKey<RawClientHttpRequestSettings>(PropertyName), this);

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

            if (!request.Headers.Contains(Internal.RawHttpHeaders.SessionHeader))
            {
                request.Headers.Add(Internal.RawHttpHeaders.SessionHeader, this.SessionId.ToString("D"));
            }

            return true;
        }

        /// <summary>
        /// Creates an instance of the default request settings.
        /// </summary>
        /// <returns>The default request settings</returns>
        private static RawClientHttpRequestSettings ConstructDefaultSettings()
        {
            // Set up reasonable defaults in case the registry keys are not present
            var settings = new RawClientHttpRequestSettings();
            settings.UserAgent = UserAgentUtility.GetDefaultRestUserAgent();

            return settings;
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

        private static Lazy<RawClientHttpRequestSettings> s_defaultSettings
            = new Lazy<RawClientHttpRequestSettings>(ConstructDefaultSettings);

        private Int32 m_maxContentBufferSize;
        // We will buffer a maximum of 1024MB in the message handler
        private const Int32 c_maxAllowedContentBufferSize = 1024 * 1024 * 1024;

        // We will buffer, by default, up to 512MB in the message handler
        private const Int32 c_defaultContentBufferSize = 1024 * 1024 * 512;

        private const Int32 c_defaultMaxRetry = 3;
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100); //default WebAPI timeout
        private ICollection<CultureInfo> m_acceptLanguages = new List<CultureInfo>();
    }
}
