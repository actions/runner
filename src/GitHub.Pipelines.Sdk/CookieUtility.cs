using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Client
{
    internal static class CookieUtility
    {
        public static readonly String AcsMetadataRetrievalExceptionText = "Unable to retrieve ACS Metadata from '{0}'";
        
        public static readonly String FedAuthCookieName = "FedAuth";
        public static readonly String WindowsLiveSignOutUrl = "https://login.live.com/uilogout.srf";
        public static readonly Uri WindowsLiveCookieDomain = new Uri("https://login.live.com/");

        public static CookieCollection GetFederatedCookies(Uri cookieDomainAndPath)
        {
            CookieCollection result = null;

            Cookie cookie = GetCookieEx(cookieDomainAndPath, FedAuthCookieName).FirstOrDefault();

            if (cookie != null)
            {
                result = new CookieCollection();
                result.Add(cookie);

                for (Int32 x = 1; x < 50; x++)
                {
                    String cookieName = FedAuthCookieName + x;
                    cookie = GetCookieEx(cookieDomainAndPath, cookieName).FirstOrDefault();

                    if (cookie != null)
                    {
                        result.Add(cookie);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public static CookieCollection GetFederatedCookies(String[] token)
        {
            CookieCollection result = null;

            if (token != null && token.Length > 0 && token[0] != null)
            {
                result = new CookieCollection();
                result.Add(new Cookie(FedAuthCookieName, token[0]));

                for (Int32 x = 1; x < token.Length; x++)
                {
                    String cookieName = FedAuthCookieName + x;

                    if (token[x] != null)
                    {
                        Cookie cookie = new Cookie(cookieName, token[x]);
                        cookie.HttpOnly = true;
                        result.Add(cookie);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public static CookieCollection GetFederatedCookies(IHttpResponse webResponse)
        {
            CookieCollection result = null;
            IEnumerable<String> cookies = null;

            if (webResponse.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                foreach (String cookie in cookies)
                {
                    if (cookie != null && cookie.StartsWith(CookieUtility.FedAuthCookieName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Only take the security token field of the cookie, and discard the rest
                        String fedAuthToken = cookie.Split(';').FirstOrDefault();
                        Int32 index = fedAuthToken.IndexOf('=');

                        if (index > 0 && index < fedAuthToken.Length - 1)
                        {
                            String name = fedAuthToken.Substring(0, index);
                            String value = fedAuthToken.Substring(index + 1);

                            result = result ?? new CookieCollection();
                            result.Add(new Cookie(name, value));
                        }
                    }
                }
            }

            return result;
        }

        public static CookieCollection GetAllCookies(Uri cookieDomainAndPath)
        {
            CookieCollection result = null;
            List<Cookie> cookies = GetCookieEx(cookieDomainAndPath, null);
            foreach (Cookie cookie in cookies)
            {
                if (result == null)
                {
                    result = new CookieCollection();
                }

                result.Add(cookie);
            }

            return result;
        }

        public static void DeleteFederatedCookies(Uri cookieDomainAndPath)
        {
            CookieCollection cookies = GetFederatedCookies(cookieDomainAndPath);

            if (cookies != null)
            {
                foreach (Cookie cookie in cookies)
                {
                    DeleteCookieEx(cookieDomainAndPath, cookie.Name);
                }
            }
        }

        public static void DeleteWindowsLiveCookies()
        {
            DeleteAllCookies(WindowsLiveCookieDomain);
        }

        public static void DeleteAllCookies(Uri cookieDomainAndPath)
        {
            CookieCollection cookies = GetAllCookies(cookieDomainAndPath);

            if (cookies != null)
            {
                foreach (Cookie cookie in cookies)
                {
                    DeleteCookieEx(cookieDomainAndPath, cookie.Name);
                }
            }
        }

        public const UInt32 INTERNET_COOKIE_HTTPONLY = 0x00002000;

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool InternetGetCookieEx(
        String url, String cookieName, StringBuilder cookieData, ref Int32 size, UInt32 flags, IntPtr reserved);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool InternetSetCookieEx(
        String url, String cookieName, String cookieData, UInt32 flags, IntPtr reserved);

        public static Boolean DeleteCookieEx(Uri cookiePath, String cookieName)
        {
            UInt32 flags = INTERNET_COOKIE_HTTPONLY;

            String path = cookiePath.ToString();
            if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                path = path + "/";
            }

            DateTime expiration = DateTime.UtcNow.AddYears(-1);
            String cookieData = String.Format(CultureInfo.InvariantCulture, "{0}=0;expires={1};path=/;domain={2};httponly", cookieName, expiration.ToString("R"), cookiePath.Host);

            return InternetSetCookieEx(path, null, cookieData, flags, IntPtr.Zero);
        }

        public static Boolean SetCookiesEx(
            Uri cookiePath,
            CookieCollection cookies)
        {
            String path = cookiePath.ToString();
            if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                path = path + "/";
            }

            Boolean successful = true;
            foreach (Cookie cookie in cookies)
            {
                // This means it doesn't expire
                if (cookie.Expires.Year == 1)
                {
                    continue;
                }

                String cookieData = String.Format(CultureInfo.InvariantCulture,
                                                  "{0}; path={1}; domain={2}; expires={3}; httponly",
                                                  cookie.Value,
                                                  cookie.Path,
                                                  cookie.Domain,
                                                  cookie.Expires.ToString("ddd, dd-MMM-yyyy HH:mm:ss 'GMT'"));

                successful &= InternetSetCookieEx(path, cookie.Name, cookieData, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero);
            }
            return successful;
        }

        public static List<Cookie> GetCookieEx(Uri cookiePath, String cookieName)
        {
            UInt32 flags = INTERNET_COOKIE_HTTPONLY;

            List<Cookie> cookies = new List<Cookie>();
            Int32 size = 256;
            StringBuilder cookieData = new StringBuilder(size);
            String path = cookiePath.ToString();
            if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                path = path + "/";
            }

            if (!InternetGetCookieEx(path, cookieName, cookieData, ref size, flags, IntPtr.Zero))
            {
                if (size < 0)
                {
                    return cookies;
                }

                cookieData = new StringBuilder(size);

                if (!InternetGetCookieEx(path, cookieName, cookieData, ref size, flags, IntPtr.Zero))
                {
                    return cookies;
                }
            }

            if (cookieData.Length > 0)
            {
                String[] cookieSections = cookieData.ToString().Split(new char[] { ';' });

                foreach (String cookieSection in cookieSections)
                {
                    String[] cookieParts = cookieSection.Split(new char[] { '=' }, 2);

                    if (cookieParts.Length == 2)
                    {
                        Cookie cookie = new Cookie();
                        cookie.Name = cookieParts[0].TrimStart();
                        cookie.Value = cookieParts[1];
                        cookie.HttpOnly = true;
                        cookies.Add(cookie);
                    }
                }
            }

            return cookies;
        }
    }
}
