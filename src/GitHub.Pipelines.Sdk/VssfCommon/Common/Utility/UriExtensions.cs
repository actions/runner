using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace GitHub.Services.Common
{
    public static class UriExtensions
    {
        public static Uri AppendQuery(this Uri uri, String name, String value)
        {
            ArgumentUtility.CheckForNull(uri, "uri");
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            ArgumentUtility.CheckStringForNullOrEmpty(value, "value");

            StringBuilder stringBuilder = new StringBuilder(uri.Query.TrimStart('?'));

            AppendSingleQueryValue(stringBuilder, name, value);

            UriBuilder uriBuilder = new UriBuilder(uri);

            uriBuilder.Query = stringBuilder.ToString();

            return uriBuilder.Uri;
        }

        public static Uri AppendQuery(this Uri uri, IEnumerable<KeyValuePair<String, String>> queryValues)
        {
            ArgumentUtility.CheckForNull(uri, "uri");
            ArgumentUtility.CheckForNull(queryValues, "queryValues");

            StringBuilder stringBuilder = new StringBuilder(uri.Query.TrimStart('?'));

            foreach (KeyValuePair<String, String> kvp in queryValues)
            {
                AppendSingleQueryValue(stringBuilder, kvp.Key, kvp.Value);
            }

            UriBuilder uriBuilder = new UriBuilder(uri);
            uriBuilder.Query = stringBuilder.ToString();
            return uriBuilder.Uri;
        }

        public static Uri AppendQuery(this Uri uri, NameValueCollection queryValues)
        {
            ArgumentUtility.CheckForNull(uri, "uri");
            ArgumentUtility.CheckForNull(queryValues, "queryValues");

            StringBuilder stringBuilder = new StringBuilder(uri.Query.TrimStart('?'));

            foreach (String name in queryValues)
            {
                AppendSingleQueryValue(stringBuilder, name, queryValues[name]);
            }

            UriBuilder uriBuilder = new UriBuilder(uri);

            uriBuilder.Query = stringBuilder.ToString();

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Performs an Add similar to the NameValuCollection 'Add' method where the value gets added as an item in a comma delimited list if the key is already present.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="convert"></param>
        public static void Add<T>(this IList<KeyValuePair<String, String>> collection, String key, T value, Func<T, String> convert = null)
        {
            collection.AddMultiple<T>(key, new List<T> { value }, convert);
        }

        public static void AddMultiple<T>(this IList<KeyValuePair<String, String>> collection, String key, IEnumerable<T> values, Func<T, String> convert)
        {
            ArgumentUtility.CheckForNull(collection, "collection");
            ArgumentUtility.CheckStringForNullOrEmpty(key, "name");

            if (convert == null) convert = (val) => val.ToString();

            if (values != null && values.Any())
            {
                StringBuilder newValue = new StringBuilder();
                KeyValuePair<String, String> matchingKvp = collection.FirstOrDefault(kvp => kvp.Key.Equals(key));
                if (matchingKvp.Key == key)
                {
                    collection.Remove(matchingKvp);
                    newValue.Append(matchingKvp.Value);
                }

                foreach (var value in values)
                {
                    if (newValue.Length > 0)
                    {
                        newValue.Append(",");
                    }
                    newValue.Append(convert(value));
                }

                collection.Add(new KeyValuePair<String, String>(key, newValue.ToString()));
            }
        }

        public static void Add(this IList<KeyValuePair<String, String>> collection, String key, String value)
        {
            collection.AddMultiple(key, new[] { value });
        }

        public static void AddMultiple(this IList<KeyValuePair<String, String>> collection, String key, IEnumerable<String> values)
        {
            collection.AddMultiple(key, values, (val) => val);
        }

        public static void AddMultiple<T>(this NameValueCollection collection, String name, IEnumerable<T> values, Func<T, String> convert)
        {
            ArgumentUtility.CheckForNull(collection, "collection");
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");

            if (convert == null) convert = (val) => val.ToString();

            if (values != null)
            {
                foreach (var value in values)
                {
                    collection.Add(name, convert(value));
                }
            }
        }

        public static void AddMultiple(this NameValueCollection collection, String name, IEnumerable<String> values)
        {
            ArgumentUtility.CheckForNull(collection, "collection");
            collection.AddMultiple(name, values, (val) => val);
        }

        /// <summary>
        /// Get the absolute path of the given Uri, if it is absolute.
        /// </summary>
        /// <returns>If the URI is absolute, the string form of it is returned; otherwise,
        /// the URI's string representation.</returns>
        public static string AbsoluteUri(this Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
        }

        private static void AppendSingleQueryValue(StringBuilder builder, String name, String value)
        {
            if (builder.Length > 0)
            {
                builder.Append("&");
            }
            builder.Append(Uri.EscapeDataString(name));
            builder.Append("=");
            builder.Append(Uri.EscapeDataString(value));
        }
    }
}
