#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public class RunsOn
    {
        public HashSet<string> Labels
        {
            get
            {
                if (m_labels == null)
                {
                    m_labels = new HashSet<string>();
                }
                return m_labels;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String RunnerGroup { get; set; }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_labels?.Count == 0)
            {
                m_labels = null;
            }
        }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        private HashSet<string> m_labels;
    }
}
