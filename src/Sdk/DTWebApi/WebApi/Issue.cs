using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class Issue
    {

        public Issue()
        {
        }

        private Issue(Issue issueToBeCloned)
        {
            this.Type = issueToBeCloned.Type;
            this.Category = issueToBeCloned.Category;
            this.Message = issueToBeCloned.Message;

            if (issueToBeCloned.m_data != null)
            {
                foreach (var item in issueToBeCloned.m_data)
                {
                    this.Data.Add(item);
                }
            }
        }

        [DataMember(Order = 1)]
        public IssueType Type
        {
            get;
            set;
        }

        [DataMember(Order = 2)]
        public String Category
        {
            get;
            set;
        }

        [DataMember(Order = 3)]
        public String Message
        {
            get;
            set;
        }

        public IDictionary<String, String> Data
        {
            get
            {
                if (m_data == null)
                {
                    m_data = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_data;
            }
        }

        public Issue Clone()
        {
            return new Issue(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedData, ref m_data, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_data, ref m_serializedData, StringComparer.OrdinalIgnoreCase);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedData = null;
        }

        [DataMember(Name = "Data", EmitDefaultValue = false, Order = 4)]
        private IDictionary<String, String> m_serializedData;

        private IDictionary<String, String> m_data;
    }
}
