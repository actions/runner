using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common.Diagnostics;
using Microsoft.VisualStudio.Services.Common.Internal;

namespace Microsoft.VisualStudio.Services.Common
{
    internal sealed class VssServiceIdentityTokenProvider : IssuedTokenProvider
    {
        public VssServiceIdentityTokenProvider(
            VssServiceIdentityCredential credential,
            Uri serverUrl,
            Uri signInUrl,
            string realm,
            DelegatingHandler innerHandler)
            : this(credential, serverUrl, signInUrl, realm)
        {
            m_innerHandler = innerHandler;
        }

        public VssServiceIdentityTokenProvider(
            VssServiceIdentityCredential credential,
            Uri serverUrl,
            Uri signInUrl,
            string realm)
            : base(credential, serverUrl, signInUrl)
        {
            Realm = realm;
        }

        protected override string AuthenticationParameter
        {
            get
            {
                if (string.IsNullOrEmpty(this.Realm) && this.SignInUrl == null)
                {
                    return string.Empty;
                }
                else
                {
                    return string.Format(CultureInfo.InvariantCulture, "issuer=\"{0}\", realm=\"{1}\"", this.SignInUrl, this.Realm);
                }
            }
        }

        protected override String AuthenticationScheme
        {
            get
            {
                return "TFS-Federated";
            }
        }

        /// <summary>
        /// Gets the simple web token credential from which this provider was created.
        /// </summary>
        public new VssServiceIdentityCredential Credential
        {
            get
            {
                return (VssServiceIdentityCredential)base.Credential;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not a call to get token will require interactivity.
        /// </summary>
        public override Boolean GetTokenIsInteractive
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the realm for the token provider.
        /// </summary>
        public String Realm
        {
            get;
        }

        protected internal override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (!base.IsAuthenticationChallenge(webResponse))
            {
                return false;
            }

            // This means we were proactively constructed without any connection information. In this case
            // we return false to ensure that a new provider is reconstructed with all appropriate configuration
            // to retrieve a new token.
            if (this.SignInUrl == null)
            {
                return false;
            }

            string authRealm = webResponse.Headers.GetValues(HttpHeaders.TfsFedAuthRealm).FirstOrDefault();
            string authIssuer = webResponse.Headers.GetValues(HttpHeaders.TfsFedAuthIssuer).FirstOrDefault();
            Uri signInUrl = new Uri(new Uri(authIssuer).GetLeftPart(UriPartial.Authority), UriKind.Absolute);

            // Make sure that the values match our stored values. This way if the values change we will be thrown
            // away and a new instance with correct values will be constructed.
            return this.Realm.Equals(authRealm, StringComparison.OrdinalIgnoreCase) &&
                   Uri.Compare(this.SignInUrl, signInUrl, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Issues a request to synchronously retrieve a token for the associated credential.
        /// </summary>
        /// <param name="failedToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<IssuedToken> OnGetTokenAsync(
            IssuedToken failedToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.Credential.UserName) ||
                string.IsNullOrEmpty(this.Credential.Password))
            {
                return null;
            }

            VssTraceActivity traceActivity = VssTraceActivity.Current;
            using (HttpClient client = new HttpClient(CreateMessageHandler(), false))
            {
                client.BaseAddress = this.SignInUrl;

                KeyValuePair<string, string>[] values = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("wrap_name", this.Credential.UserName),
                    new KeyValuePair<string, string>("wrap_password", this.Credential.Password),
                    new KeyValuePair<string, string>("wrap_scope", this.Realm),
                };

                Uri url = new Uri("WRAPv0.9/", UriKind.Relative);
                FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                using (HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                {
                    string responseValue = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return VssServiceIdentityToken.ExtractToken(responseValue);
                    }
                    else
                    {
                        VssHttpEventSource.Log.AuthenticationError(traceActivity, this, responseValue);
                        return null;
                    }
                }
            }
        }

        private HttpMessageHandler CreateMessageHandler()
        {
            var retryOptions = new VssHttpRetryOptions()
            {
                RetryableStatusCodes =
                {
                    VssNetworkHelper.TooManyRequests,
                    HttpStatusCode.InternalServerError,
                },
            };

            HttpMessageHandler innerHandler;

            if (m_innerHandler != null)
            {
                if (m_innerHandler.InnerHandler == null)
                {
                    m_innerHandler.InnerHandler = new HttpClientHandler();
                }

                innerHandler = m_innerHandler;
            }
            else
            {
                innerHandler = new HttpClientHandler();
            }

            // Inherit proxy setting from VssHttpMessageHandler
            var httpClientHandler = innerHandler as HttpClientHandler;
            if (httpClientHandler != null && VssHttpMessageHandler.DefaultWebProxy != null)
            {
                httpClientHandler.Proxy = VssHttpMessageHandler.DefaultWebProxy;
                httpClientHandler.UseProxy = true;
            }

            return new VssHttpRetryMessageHandler(retryOptions, innerHandler);
        }

        private DelegatingHandler m_innerHandler = null;
    }
}
