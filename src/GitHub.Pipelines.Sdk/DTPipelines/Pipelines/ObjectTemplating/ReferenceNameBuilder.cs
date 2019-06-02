using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating
{
    internal sealed class ReferenceNameBuilder
    {
        internal void AppendSegment(String value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return;
            }

            if (m_name.Length == 0)
            {
                var first = value[0];
                if ((first >= 'a' && first <= 'z') ||
                    (first >= 'A' && first <= 'Z') ||
                    first == '_')
                {
                    // Legal first char
                }
                else if (first >= '0' && first <= '9') // todo: support '-'
                {
                    // Illegal first char, but legal char.
                    // Prepend "_".
                    m_name.Append("_");
                }
                else
                {
                    // Illegal char
                }
            }
            else
            {
                // Separator
                m_name.Append(c_separator);
            }

            foreach (var c in value)
            {
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_')  // todo: support '-'
                {
                    // Legal
                    m_name.Append(c);
                }
                else
                {
                    // Illegal
                    m_name.Append("_");
                }
            }
        }

        internal String Build()
        {
            var original = m_name.Length > 0 ? m_name.ToString() : "job";

            var attempt = 1;
            var suffix = default(String);
            while (true)
            {
                if (attempt == 1)
                {
                    suffix = String.Empty;
                }
                else if (attempt < 1000)
                {
                    suffix = String.Format(CultureInfo.InvariantCulture, "_{0}", attempt);
                }
                else
                {
                    throw new InvalidOperationException("Unable to create a unique name");
                }

                var candidate = original.Substring(0, Math.Min(original.Length, PipelineConstants.MaxNodeNameLength - suffix.Length)) + suffix;

                if (m_distinctNames.Add(candidate))
                {
                    m_name.Clear();
                    return candidate;
                }

                attempt++;
            }
        }

        private const String c_separator = "_";
        private readonly HashSet<String> m_distinctNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        private readonly StringBuilder m_name = new StringBuilder();
    }
}
