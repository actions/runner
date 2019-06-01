using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating
{
    internal sealed class JobDisplayNameBuilder
    {
        public JobDisplayNameBuilder(String jobFactoryDisplayName)
        {
            if (!String.IsNullOrEmpty(jobFactoryDisplayName))
            {
                m_jobFactoryDisplayName = jobFactoryDisplayName;
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
            if (String.IsNullOrEmpty(m_jobFactoryDisplayName))
            {
                return null;
            }

            var displayName = default(String);
            if (m_segments.Count == 0)
            {
                displayName = m_jobFactoryDisplayName;
            }
            else
            {
                var joinedSegments = String.Join(", ", m_segments);
                displayName = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", m_jobFactoryDisplayName, joinedSegments);
            }

            const Int32 maxDisplayNameLength = 100;
            if (displayName.Length > maxDisplayNameLength)
            {
                displayName = displayName.Substring(0, maxDisplayNameLength - 3) + "...";
            }

            m_segments.Clear();
            return displayName;
        }

        private readonly String m_jobFactoryDisplayName;
        private readonly List<String> m_segments;
    }
}
