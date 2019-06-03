using System;
using System.Linq;
using System.Text;
using GitHub.Services.Common.Internal;
using System.ComponentModel;

namespace GitHub.Services.Common
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Replaces occurrences of <paramref name="oldValue"/> found in <paramref name="input"/> with 
        /// <paramref name="newValue"/>.
        /// </summary>
        /// <param name="input">The input value which should be replaced</param>
        /// <param name="oldValue">The pattern to replace</param>
        /// <param name="newValue">The replacement value to use</param>
        /// <param name="comparison">The comparison operator to use when searching for <paramref name="oldValue"/></param>
        /// <returns>A <c>String</c> with occurrences of <paramref name="oldValue"/> replaced with <paramref name="newValue"/></returns>
        public static String Replace(
            this String input,
            String oldValue,
            String newValue,
            StringComparison comparison)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(oldValue, nameof(oldValue));
            ArgumentUtility.CheckForNull(newValue, nameof(newValue));

            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Loop over the indexes where we find matches and append the replacement to the target, skipping over the 
            // pattern in the source. The resulting string will be the replaced value.
            int startIndex = 0;
            int matchIndex = -1;
            StringBuilder sb = null;
            while ((matchIndex = input.IndexOf(oldValue, startIndex, comparison)) >= 0)
            {
                if (sb == null)
                {
                    // This is an optimization for the case that we are replacing the entire string. We can avoid the
                    // string builder allocation in this case by simply returning the new value.
                    if (matchIndex == 0 && input.Length == oldValue.Length)
                    {
                        input = newValue;
                        break;
                    }
                    else
                    {
                        sb = new StringBuilder(input.Substring(0, matchIndex));
                    }
                }
                else if (matchIndex > startIndex)
                {
                    sb.Append(input.Substring(startIndex, matchIndex - startIndex));
                }

                sb.Append(newValue);
                startIndex = matchIndex + oldValue.Length;
            }

            // If anything was replaced we will have allocated a string builder. Otherwise it will remain null and
            // we should just return the original value.
            if (sb != null)
            {
                // If there were any replacements done we need to make sure we copy the rest of the rest of the string
                // if we aren't at the end
                if (startIndex < input.Length)
                {
                    sb.Append(input.Substring(startIndex));
                }

                return sb.ToString();
            }
            else
            {
                return input;
            }
        }

        public static String UnescapeXml(this String source)
        {
            if (String.IsNullOrEmpty(source) || !source.Contains('&'))
            {
                return source;
            }
            else
            {
                StringBuilder sb = new StringBuilder(source);
                sb.Replace("&lt;", "<");
                sb.Replace("&gt;", ">");
                sb.Replace("&amp;", "&");
                sb.Replace("&apos;", "'");
                sb.Replace("&quot;", "\"");
                return sb.ToString();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Boolean IsSubdomainOf(this String domain, String parentDomain)
        {
            return UriUtility.IsSubdomainOf(domain, parentDomain);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Uri AsUri(this string uri) => new Uri(uri);
    }
}
