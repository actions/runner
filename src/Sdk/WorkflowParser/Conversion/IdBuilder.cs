#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Builder for job and step IDs
    /// </summary>
    internal sealed class IdBuilder
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
                else if ((first >= '0' && first <= '9') || first == '-')
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
                    c == '_' ||
                    c == '-')
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

        /// <summary>
        /// Builds the ID from the segments
        /// </summary>
        /// <param name="allowReservedPrefix">When true, generated IDs may begin with "__" depending upon the segments
        /// and collisions with known IDs. When false, generated IDs will never begin with the reserved prefix "__".</param>
        /// <param name="maxLength">The maximum length of the generated ID.</param>
        internal String Build(
            Boolean allowReservedPrefix,
            Int32 maxLength = WorkflowConstants.MaxNodeNameLength)
        {
            // Ensure reasonable max length
            if (maxLength <= 5) // Must be long enough to accommodate at least one character + length of max suffix "_999" (refer suffix logic further below)
            {
                maxLength = WorkflowConstants.MaxNodeNameLength;
            }

            var original = m_name.Length > 0 ? m_name.ToString() : "job";

            // Avoid prefix "__" when not allowed
            if (!allowReservedPrefix && original.StartsWith("__", StringComparison.Ordinal))
            {
                original = $"_{original.TrimStart('_')}";
            }

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
                    // Special case to avoid prefix "__" when not allowed
                    if (!allowReservedPrefix && String.Equals(original, "_", StringComparison.Ordinal))
                    {
                        suffix = String.Format(CultureInfo.InvariantCulture, "{0}", attempt);
                    }
                    else
                    {
                        suffix = String.Format(CultureInfo.InvariantCulture, "_{0}", attempt);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unable to create a unique name");
                }

                var candidate = original.Substring(0, Math.Min(original.Length, maxLength - suffix.Length)) + suffix;

                if (m_distinctNames.Add(candidate))
                {
                    m_name.Clear();
                    return candidate;
                }

                attempt++;
            }
        }

        internal Boolean TryAddKnownId(
            String value,
            out String error)
        {
            if (String.IsNullOrEmpty(value) ||
                !IsValid(value) ||
                value.Length >= WorkflowConstants.MaxNodeNameLength)
            {
                error = $"The identifier '{value}' is invalid. IDs may only contain alphanumeric characters, '_', and '-'. IDs must start with a letter or '_' and must be less than {WorkflowConstants.MaxNodeNameLength} characters.";
                return false;
            }
            else if (value.StartsWith("__", StringComparison.Ordinal))
            {
                error = $"The identifier '{value}' is invalid. IDs starting with '__' are reserved.";
                return false;
            }
            else if (!m_distinctNames.Add(value))
            {
                error = $"The identifier '{value}' may not be used more than once within the same scope.";
                return false;
            }
            else
            {
                error = null;
                return true;
            }
        }

        private static Boolean IsValid(String name)
        {
            var result = true;
            for (Int32 i = 0; i < name.Length; i++)
            {
                if ((name[i] >= 'a' && name[i] <= 'z') ||
                    (name[i] >= 'A' && name[i] <= 'Z') ||
                    (name[i] >= '0' && name[i] <= '9' && i > 0) ||
                    (name[i] == '_') ||
                    (name[i] == '-' && i > 0))
                {
                    continue;
                }
                else
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private const String c_separator = "_";
        private readonly HashSet<String> m_distinctNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        private readonly StringBuilder m_name = new StringBuilder();
    }
}
