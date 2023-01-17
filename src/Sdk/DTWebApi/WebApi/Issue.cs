using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{

    public interface IReadOnlyIssue
    {
        IssueType Type { get; }
        string Category { get; }
        string Message { get; }
        bool? IsInfrastructureIssue { get; }
        string this[string key] { get; }
    }

    public class IssueMetadata
    {

        public IssueMetadata(string key, string value)
            : this(null, false, new []{ KeyValuePair.Create(key, value) })
        {
        }

        public IssueMetadata(string category, bool infrastructureIssue, IEnumerable<KeyValuePair<string, string>> data)
        {
            this.Category = category;
            this.IsInfrastructureIssue = infrastructureIssue;
            this.Data = data;
        }


        public readonly string Category;
        public readonly bool IsInfrastructureIssue;
        public readonly IEnumerable<KeyValuePair<string, string>> Data;
    }

    [DataContract]
    public class Issue : IReadOnlyIssue
    {

        public Issue()
        {
        }

        private Issue(Issue original)
        {
            this.Type = original.Type;
            this.Category = original.Category;
            this.Message = original.Message;
            this.IsInfrastructureIssue = original.IsInfrastructureIssue;

            if (original.m_data != null)
            {
                foreach (var item in original.m_data)
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

        [DataMember(Order = 4)]
        public bool? IsInfrastructureIssue
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

        public string this[string key]
        {
            get
            {
                m_data.TryGetValue(key, out string result);
                return result;
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
