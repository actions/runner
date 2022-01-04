using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GitHub.Services.WebApi
{
    public static class VssHttpUriUtility
    {
        /// <summary>
        /// Replace values in a templated route with the given route values dictionary.
        /// </summary>
        /// <param name="routeTemplate"></param>
        /// <param name="routeValues"></param>
        /// <param name="escapeUri">Set true to escape the replaced route Uri string prior to returning it</param>
        /// <param name="appendUnusedAsQueryParams">Set true to append any unused routeValues as query parameters to the returned route</param>
        /// <param name="requireExplicitRouteParams">If set to true requires all the route parameters to be explicitly passed in routeParams</param>
        /// <returns></returns>
        public static String ReplaceRouteValues(
            String routeTemplate,
            Dictionary<String, Object> routeValues,
            bool escapeUri = false,
            bool appendUnusedAsQueryParams = false,
            bool requireExplicitRouteParams = false)
        {
            RouteReplacementOptions routeReplacementOptions = escapeUri ? RouteReplacementOptions.EscapeUri : 0;
            routeReplacementOptions |= appendUnusedAsQueryParams ? RouteReplacementOptions.AppendUnusedAsQueryParams : 0;
            routeReplacementOptions |= requireExplicitRouteParams ? RouteReplacementOptions.RequireExplicitRouteParams : 0;

            return ReplaceRouteValues(
                routeTemplate, 
                routeValues,
                routeReplacementOptions);
        }

        /// <summary>
        /// Replace values in a templated route with the given route values dictionary.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static String ReplaceRouteValues(
            String routeTemplate,
            Dictionary<String, Object> routeValues,
            RouteReplacementOptions routeReplacementOptions)
        {
            StringBuilder sbResult = new StringBuilder();
            StringBuilder sbCurrentPathPart = new StringBuilder();
            int paramStart = -1, paramLength = 0;
            bool insideParam = false;
            HashSet<string> unusedValues = new HashSet<string>(routeValues.Keys, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, object> caseIncensitiveRouteValues = new Dictionary<string, object>(routeValues, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < routeTemplate.Length; i++)
            {
                char c = routeTemplate[i];

                if (insideParam)
                {
                    if (c == '}')
                    {
                        insideParam = false;
                        String paramName = routeTemplate.Substring(paramStart, paramLength);
                        paramLength = 0;
                        if (paramName.StartsWith("*"))
                        {
                            if (routeReplacementOptions.HasFlag(RouteReplacementOptions.WildcardAsQueryParams))
                            {
                                continue;
                            }
                            // wildcard route
                            paramName = paramName.Substring(1);
                        }

                        Object paramValue;
                        if (caseIncensitiveRouteValues.TryGetValue(paramName, out paramValue))
                        {
                            if (paramValue != null)
                            {
                                sbCurrentPathPart.Append(paramValue.ToString());
                                unusedValues.Remove(paramName);
                            }
                        }
                        else if (routeReplacementOptions.HasFlag(RouteReplacementOptions.RequireExplicitRouteParams))
                        {
                            throw new ArgumentException("Missing route param " + paramName);
                        }
                    }
                    else
                    {
                        paramLength++;
                    }
                }
                else
                {
                    if (c == '/')
                    {
                        if (sbCurrentPathPart.Length > 0)
                        {
                            sbResult.Append('/');
                            sbResult.Append(sbCurrentPathPart.ToString());
                            sbCurrentPathPart.Clear();
                        }
                    }
                    else if (c == '{')
                    {
                        if ((i + 1) < routeTemplate.Length && routeTemplate[i + 1] == '{')
                        {
                            // Escaped '{'
                            sbCurrentPathPart.Append(c);
                            i++;
                        }
                        else
                        {
                            insideParam = true;
                            paramStart = i + 1;
                        }
                    }
                    else if (c == '}')
                    {
                        sbCurrentPathPart.Append(c);
                        if ((i + 1) < routeTemplate.Length && routeTemplate[i + 1] == '}')
                        {
                            // Escaped '}'
                            i++;
                        }
                    }
                    else
                    {
                        sbCurrentPathPart.Append(c);
                    }
                }
            }

            if (sbCurrentPathPart.Length > 0)
            {
                sbResult.Append('/');
                sbResult.Append(sbCurrentPathPart.ToString());
            }

            if (routeReplacementOptions.HasFlag(RouteReplacementOptions.EscapeUri))
            {
                sbResult = new StringBuilder(Uri.EscapeUriString(sbResult.ToString()));
            }

            if (routeReplacementOptions.HasFlag(RouteReplacementOptions.AppendUnusedAsQueryParams) && unusedValues.Count > 0)
            {
                bool isFirst = true;

                foreach (String paramName in unusedValues)
                {
                    Object paramValue;
                    if (caseIncensitiveRouteValues.TryGetValue(paramName, out paramValue) && paramValue != null)
                    {
                        sbResult.Append(isFirst ? '?' : '&');
                        isFirst = false;
                        sbResult.Append(Uri.EscapeDataString(paramName));
                        sbResult.Append('=');
                        sbResult.Append(Uri.EscapeDataString(paramValue.ToString()));
                    }
                }
            }

            return sbResult.ToString();
        }

        /// <summary>
        /// Create a route values dictionary, and add the specified area and resource if they aren't present.
        /// </summary>
        /// <param name="routeValues"></param>
        /// <param name="area">Area name</param>
        /// <param name="resourceName">Resource name</param>
        /// <returns></returns>
        public static Dictionary<String, Object> ToRouteDictionary(Object routeValues, string area, string resourceName)
        {
            Dictionary<String, Object> valuesDictionary = VssHttpUriUtility.ToRouteDictionary(routeValues);
            VssHttpUriUtility.AddRouteValueIfNotPresent(valuesDictionary, "area", area);
            VssHttpUriUtility.AddRouteValueIfNotPresent(valuesDictionary, "resource", resourceName);

            return valuesDictionary;
        }

        public static Uri ConcatUri(Uri baseUri, String relativeUri)
        {
            StringBuilder sbCombined = new StringBuilder(baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/'));
            sbCombined.Append('/');
            sbCombined.Append(relativeUri.TrimStart('/'));
            sbCombined.Append(baseUri.Query);
            return new Uri(sbCombined.ToString());
        }

        public static Dictionary<String, Object> ToRouteDictionary(Object values)
        {
            if (values == null)
            {
                return new Dictionary<String, Object>();
            }
            else if (values is Dictionary<String, Object>)
            {
                return (Dictionary<String, Object>)values;
            }
            else
            {
                Dictionary<String, Object> dictionary = new Dictionary<String, Object>();
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(values))
                {
                    dictionary[descriptor.Name] = descriptor.GetValue(values);
                }
                return dictionary;
            }
        }
        private static void AddRouteValueIfNotPresent(Dictionary<String, Object> dictionary, String key, Object value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Flags]
    public enum RouteReplacementOptions
    {
        None = 0,
        EscapeUri = 1,
        AppendUnusedAsQueryParams = 2,
        RequireExplicitRouteParams = 4,
        WildcardAsQueryParams = 8,
    }
}
