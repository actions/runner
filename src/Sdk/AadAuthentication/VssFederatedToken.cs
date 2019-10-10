using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using GitHub.Services.Common;

namespace GitHub.Services.Client
{
    /// <summary>
    /// Provides a cookie-based authentication token.
    /// </summary>
    [Serializable]
    public sealed class VssFederatedToken : IssuedToken
    {
        /// <summary>
        /// Initializes a new <c>VssFederatedToken</c> instance using the specified cookies.
        /// </summary>
        /// <param name="cookies"></param>
        public VssFederatedToken(CookieCollection cookies)
        {
            ArgumentUtility.CheckForNull(cookies, "cookies");
            m_cookies = cookies;
        }

        /// <summary>
        /// Returns the CookieCollection contained within this token. For internal use only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CookieCollection CookieCollection
        {
            get 
            { 
                return m_cookies; 
            }
        }

        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Federated;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            // From http://www.ietf.org/rfc/rfc2109.txt:
            //      Note: For backward compatibility, the separator in the Cookie header 
            //      is semi-colon (;) everywhere.
            //
            // HttpRequestHeaders uses comma as the default separator, so instead of returning 
            // a list of cookies, the method returns one semicolon separated string.
            IEnumerable<String> values = request.Headers.GetValues(s_cookieHeader);
            request.Headers.SetValue(s_cookieHeader, GetHeaderValue(values));
        }

        private String GetHeaderValue(IEnumerable<String> cookieHeaders)
        {
            List<String> currentCookies = new List<String>();
            if (cookieHeaders != null)
            {
                foreach (String value in cookieHeaders)
                {
                    currentCookies.AddRange(value.Split(';').Select(x => x.Trim()));
                }
            }

            currentCookies.RemoveAll(x => String.IsNullOrEmpty(x));

            foreach (Cookie cookie in m_cookies)
            {
                // Remove all existing cookies that match the name of the cookie we are going to add.
                currentCookies.RemoveAll(x => String.Equals(x.Substring(0, x.IndexOf('=')), cookie.Name, StringComparison.OrdinalIgnoreCase));
                currentCookies.Add(String.Concat(cookie.Name, "=", cookie.Value));
            }

            return String.Join("; ", currentCookies);
        }

        private CookieCollection m_cookies;
        private static readonly String s_cookieHeader = HttpRequestHeader.Cookie.ToString();
    }
}
