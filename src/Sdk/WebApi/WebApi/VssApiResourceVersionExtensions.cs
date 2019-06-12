using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Extension methods for getting/setting API resource version information from requests and to responses
    /// </summary>
    public static class ApiResourceVersionExtensions
    {
        public const String c_apiVersionHeaderKey = "api-version";
        internal const String c_legacyResourceVersionHeaderKey = "res-version";

        /// <summary>
        /// Generate version key/value pairs to use in the header, replacing any existing api-version value.
        /// </summary>
        /// <param name="headerValues">Header values to populate</param>
        /// <param name="version">Version to supply in the header</param>
        public static void AddApiResourceVersionValues(this ICollection<NameValueHeaderValue> headerValues, ApiResourceVersion version)
        {
            AddApiResourceVersionValues(headerValues, version, replaceExisting: true, useLegacyFormat: false);
        }

        /// <summary>
        /// Generate version key/value pairs to use in the header
        /// </summary>
        /// <param name="headerValues">Header values to populate</param>
        /// <param name="version">Version to supply in the header</param>
        /// <param name="replaceExisting">If true, replace an existing header with the specified version. Otherwise no-op in that case</param>
        public static void AddApiResourceVersionValues(this ICollection<NameValueHeaderValue> headerValues, ApiResourceVersion version, Boolean replaceExisting)
        {
            AddApiResourceVersionValues(headerValues, version, replaceExisting, useLegacyFormat: false);
        }

        /// <summary>
        /// Generate version key/value pairs to use in the header
        /// </summary>
        /// <param name="headerValues">Header values to populate</param>
        /// <param name="version">Version to supply in the header</param>
        /// <param name="replaceExisting">If true, replace an existing header with the specified version. Otherwise no-op in that case</param>
        /// <param name="useLegacyFormat">If true, use the legacy format of api-version combined with res-version</param>
        internal static void AddApiResourceVersionValues(this ICollection<NameValueHeaderValue> headerValues, ApiResourceVersion version, Boolean replaceExisting, Boolean useLegacyFormat)
        {
            String apiVersionHeaderValue = null;
            String resVersionHeaderValue = null;

            if (useLegacyFormat)
            {
                apiVersionHeaderValue = version.ApiVersionString;
                if (version.ResourceVersion > 0)
                {
                    resVersionHeaderValue = version.ResourceVersion.ToString();
                }
            }
            else
            {
                apiVersionHeaderValue = version.ToString();
            }

            NameValueHeaderValue existingHeader = headerValues.FirstOrDefault(h => String.Equals(c_apiVersionHeaderKey, h.Name));
            if (existingHeader != null)
            {
                if (replaceExisting)
                {
                    existingHeader.Value = apiVersionHeaderValue;
                    if (!String.IsNullOrEmpty(resVersionHeaderValue))
                    {
                        NameValueHeaderValue existingResHeader = headerValues.FirstOrDefault(h => String.Equals(c_legacyResourceVersionHeaderKey, h.Name));
                        if (existingResHeader != null)
                        {
                            existingResHeader.Value = resVersionHeaderValue;
                        }
                        else
                        {
                            headerValues.Add(new NameValueHeaderValue(c_legacyResourceVersionHeaderKey, resVersionHeaderValue));
                        }
                    }
                }
            }
            else
            {
                headerValues.Add(new NameValueHeaderValue(c_apiVersionHeaderKey, apiVersionHeaderValue));
                if (!String.IsNullOrEmpty(resVersionHeaderValue))
                {
                    headerValues.Add(new NameValueHeaderValue(c_legacyResourceVersionHeaderKey, resVersionHeaderValue));
                }
            }
        }
    }
}
