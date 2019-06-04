using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides simple web token used for OAuth authentication.
    /// </summary>
    [Serializable]
    public sealed class VssServiceIdentityToken : IssuedToken
    {
        /// <summary>
        /// Initializes a new <c>VssServiceIdentityToken</c> instance with the specified token value.
        /// </summary>
        /// <param name="token">The token value as a string</param>
        public VssServiceIdentityToken(string token)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(token, "token");

            m_token = token;
            //.ValidFrom = DateTime.UtcNow;

            // Read out the expiration time for the ValidTo field if we can find it
            Dictionary<string, string> nameValues;
            if (TryGetNameValues(token, out nameValues))
            {
                string expiresOnValue;
                if (nameValues.TryGetValue(c_expiresName, out expiresOnValue))
                {
                    // The time is represented as standard epoch
                    // base.ValidTo = s_epoch.AddSeconds(Convert.ToUInt64(expiresOnValue, CultureInfo.CurrentCulture));
                }
            }
        }

        public String Token
        {
            get
            {
                return m_token;
            }
        }

        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.ServiceIdentity;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            request.Headers.SetValue(Internal.HttpHeaders.Authorization, "WRAP access_token=\"" + m_token + "\"");
        }

        internal static VssServiceIdentityToken ExtractToken(string responseValue)
        {
            // Extract the actual token string
            string token = UriUtility.UrlDecode(responseValue
                    .Split('&')
                    .Single(value => value.StartsWith("wrap_access_token=", StringComparison.OrdinalIgnoreCase))
                    .Split('=')[1], VssHttpRequestSettings.Encoding);

            return new VssServiceIdentityToken(token);
        }

        internal static bool TryGetNameValues(
            string token, 
            out Dictionary<string, string> tokenValues)
        {
            tokenValues = null;

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            tokenValues =
                token
                .Split('&')
                .Aggregate(
                new Dictionary<string, string>(),
                (dict, rawNameValue) =>
                {
                    if (rawNameValue == string.Empty)
                    {
                        return dict;
                    }

                    string[] nameValue = rawNameValue.Split('=');

                    if (nameValue.Length != 2)
                    {
                        return dict;
                    }

                    if (dict.ContainsKey(nameValue[0]) == true)
                    {
                        return dict;
                    }

                    dict.Add(UriUtility.UrlDecode(nameValue[0]), UriUtility.UrlDecode(nameValue[1]));
                    return dict;
                });
            return true;
        }

        private string m_token;
        private const string c_expiresName = "ExpiresOn";
    }
}
