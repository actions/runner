#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Globalization;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Builder for job display names. Used when appending strategy configuration values to build a display name.
    /// </summary>
    internal sealed class JobNameBuilder
    {
        public JobNameBuilder(String jobName)
        {
            if (!String.IsNullOrEmpty(jobName))
            {
                m_jobName = jobName;
                m_segments = new List<String>();
            }
        }

        public void AppendSegment(String value)
        {
            if (String.IsNullOrEmpty(value) || m_segments == null)
            {
                return;
            }

            m_segments.Add(value);
        }

        public String Build()
        {
            if (String.IsNullOrEmpty(m_jobName))
            {
                return null;
            }

            var name = default(String);
            if (m_segments.Count == 0)
            {
                name = m_jobName;
            }
            else
            {
                var joinedSegments = String.Join(", ", m_segments);
                name = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", m_jobName, joinedSegments);
            }

            const Int32 maxNameLength = 100;
            if (name.Length > maxNameLength)
            {
                name = name.Substring(0, maxNameLength - 3) + "...";
            }

            m_segments.Clear();
            return name;
        }

        private readonly String m_jobName;
        private readonly List<String> m_segments;
    }
}
