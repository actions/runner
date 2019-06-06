using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides additional parameters for OAuth 2.0 token requests. Existing values may be removed by setting the
    /// property to null. Properties with no value should use an empty string.
    /// </summary>
    [JsonDictionary]
    public class VssOAuthTokenParameters : Dictionary<String, String>, IVssOAuthTokenParameterProvider
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenParameters</c> instance with no additional parameters.
        /// </summary>
        public VssOAuthTokenParameters()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Gets or sets the redirect_uri parameter, specifying the redirection endpoint for the user-agent after
        /// the authorization server completes interaction with the resource owner.
        /// </summary>
        public String RedirectUri
        {
            get
            {
                return GetValueOrDefault("redirect_uri");
            }
            set
            {
                RemoveOrSetValue("redirect_uri", value);
            }
        }

        /// <summary>
        /// Gets or sets the resource parameter, indicating the target resource for the token request.
        /// </summary>
        /// <remarks>
        /// At the time of writing, the specification for this parameter may be found at the link below.
        /// https://datatracker.ietf.org/doc/draft-campbell-oauth-resource-indicators/?include_text=1
        /// </remarks>
        public String Resource
        {
            get
            {
                return GetValueOrDefault("resource");
            }
            set
            {
                RemoveOrSetValue("resource", value);
            }
        }

        /// <summary>
        /// Gets or sets the scope parameter, indicating the scope of the access request.
        /// </summary>
        public String Scope
        {
            get
            {
                return GetValueOrDefault("scope");
            }
            set
            {
                RemoveOrSetValue("scope", value);
            }
        }


        /// <summary>
        /// Gets a string representation of the additional parameters as a JSON string.
        /// </summary>
        /// <returns>A string representation of the parameters which are set</returns>
        public override String ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        void IVssOAuthTokenParameterProvider.SetParameters(IDictionary<String, String> parameters)
        {
            foreach (var parameter in this)
            {
                parameters[parameter.Key] = parameter.Value;
            }
        }

        private String GetValueOrDefault(String key)
        {
            String value;
            if (!TryGetValue(key, out value))
            {
                value = null;
            }

            return value;
        }

        private void RemoveOrSetValue(
            String key,
            String value)
        {
            if (value == null)
            {
                this.Remove(key);
            }
            else
            {
                this[key] = value;
            }
        }
    }
}
