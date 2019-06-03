using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitHub.Services.Common
{
    public static class UriBuilderExtensions
    {
        /// <summary>
        /// Appends path segments to the path of <paramref name="uriBuilder"/>.
        /// </summary>
        /// <param name="uriBuilder">The <see cref="UriBuilder"/> to append to.</param>
        /// <param name="segments">The path segments to append.</param>
        /// <returns>The same instance as <paramref name="uriBuilder"/>, suitable for method chaining.</returns>
        public static UriBuilder AppendPathSegments(this UriBuilder uriBuilder, params string[] segments)
        {
            ArgumentUtility.CheckForNull(uriBuilder, nameof(uriBuilder));
            ArgumentUtility.CheckForNull(segments, nameof(segments));

            if (segments.Length == 0)
            {
                return uriBuilder;
            }

            var allSegments = new List<string>(segments.Length + 1);
            allSegments.Add(uriBuilder.Path);
            allSegments.AddRange(segments);

            // Split the incoming strings on forward-slash so that we can escape the segments.
            uriBuilder.Path = String.Join(
                "/",
                allSegments
                    .Where(s => !String.IsNullOrEmpty(s))
                    .SelectMany(s => s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim())
                    .Where(s => !String.IsNullOrEmpty(s))
                    .Select(Uri.EscapeDataString));

            return uriBuilder;
        }

        /// <summary>
        /// Append a query parameter to <paramref name="uriBuilder"/>.
        /// </summary>
        /// <param name="uriBuilder">The <see cref="UriBuilder"/> to append to.</param>
        /// <param name="name">The name of the parameter. This gets encoded before appending.</param>
        /// <param name="value">Optional. The value of the parameter. This gets encoded before appending.</param>
        /// <returns></returns>
        public static UriBuilder AppendQuery(this UriBuilder uriBuilder, string name, string value)
        {
            ArgumentUtility.CheckForNull(uriBuilder, nameof(uriBuilder));
            ArgumentUtility.CheckStringForNullOrEmpty(name, nameof(name));

            var builder = new StringBuilder(uriBuilder.Query?.TrimStart('?'));
            if (builder.Length > 0)
            {
                builder.Append("&");
            }
            uriBuilder.Query = builder
                .Append(Uri.EscapeDataString(name))
                .Append("=")
                .Append(Uri.EscapeDataString(value))
                .ToString();

            return uriBuilder;
        }

        /// <summary>
        /// Append a query parameter to <paramref name="uriBuilder"/>.
        /// </summary>
        /// <param name="uriBuilder">The <see cref="UriBuilder"/> to append to.</param>
        /// <param name="name">The name of the parameter. This does not get encoded before appending.</param>
        /// <param name="value">Optional. The value of the parameter. This does not get encoded before appending.</param>
        /// <returns></returns>
        public static UriBuilder AppendQueryEscapeUriString(this UriBuilder uriBuilder, string name, string value)
        {
            ArgumentUtility.CheckForNull(uriBuilder, nameof(uriBuilder));
            ArgumentUtility.CheckStringForNullOrEmpty(name, nameof(name));

            var builder = new StringBuilder(uriBuilder.Query?.TrimStart('?'));
            if (builder.Length > 0)
            {
                builder.Append("&");
            }
            uriBuilder.Query = builder
                .Append(Uri.EscapeUriString(name))
                .Append("=")
                .Append(Uri.EscapeUriString(value))
                .ToString();

            return uriBuilder;
        }

        /// <summary>
        /// Get the absolute URI of the given builder, if it is absolute.
        /// </summary>
        /// <param name="uriBuilder"></param>
        /// <returns>If the URI is absolute, the string form of it is returned; otherwise,
        /// the URI's string representation.</returns>
        public static string AbsoluteUri(this UriBuilder uriBuilder)
        {
            ArgumentUtility.CheckForNull(uriBuilder, nameof(uriBuilder));
            return uriBuilder.Uri.AbsoluteUri();
        }
    }
}
