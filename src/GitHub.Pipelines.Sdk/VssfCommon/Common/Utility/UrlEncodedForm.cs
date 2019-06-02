using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Microsoft.VisualStudio.Services.Common
{
    /// <summary>
    /// Content of the type <c>application/x-www-form-urlencoded</c>.
    /// </summary>
    /// <remarks>
    /// This content is essentially a query string without the beginning <c>?</c>.
    /// </remarks>
    public class UrlEncodedForm
    {
        private readonly QueryBuilder qb;

        public UrlEncodedForm(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            ArgumentUtility.CheckForNull(parameters, nameof(parameters));

            this.qb = parameters.Aggregate(new QueryBuilder(), (qb, p) => qb.Append(p.Key, p.Value));
        }

        public StringContent StringContent => new StringContent(qb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    /// <summary>
    /// Build a query string from key-value pairs.
    /// The beginning <c>?</c> is not included.
    /// </summary>
    internal class QueryBuilder
    {
        private readonly StringBuilder sb = new StringBuilder();

        /// <summary>
        /// Add a key and optional value to the working query string.
        /// </summary>
        public QueryBuilder Append(string key, string value)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(key, nameof(key));

            if (sb.Length != 0)
            {
                sb.Append("&");
            }

            sb.Append(Uri.EscapeDataString(key));

            if (!string.IsNullOrEmpty(value))
            {
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(value));
            }

            return this;
        }

        public override string ToString() => sb.ToString();
    }
}
