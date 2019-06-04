using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GitHub.DistributedTask.WebApi
{
    internal static class ListKeyValuePairExtensions
    {
        public static void AddIfNotEmpty<TValue>(this IList<KeyValuePair<String, String>> queryParameters, String parameterName, IEnumerable<TValue> values)
        {
            if (values != null && values.Any())
            {
                queryParameters.Add(new KeyValuePair<String, String>(parameterName, String.Join(",", values)));
            }
        }

        public static void AddIfNotEmpty(this IList<KeyValuePair<String, String>> queryParameters, String parameterName, String value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                queryParameters.Add(new KeyValuePair<String, String>(parameterName, value));
            }
        }

        public static void AddIfNotZero(this IList<KeyValuePair<String, String>> queryParameters, String parameterName, Int32 value)
        {
            if (value != 0)
            {
                queryParameters.Add(new KeyValuePair<String, String>(parameterName, value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public static void AddIfTrue(this IList<KeyValuePair<String, String>> queryParameters, String parameterName, Boolean value)
        {
            if (value)
            {
                queryParameters.Add(new KeyValuePair<String, String>(parameterName, value.ToString().ToLowerInvariant()));
            }
        }

        public static void AddIfNotNull<T>(this IList<KeyValuePair<String, String>> queryParameters, String parameterName, T value)
        {
            if (value != null)
            {
                queryParameters.Add(new KeyValuePair<String, String>(parameterName, value.ToString()));
            }
        }
    }
}
