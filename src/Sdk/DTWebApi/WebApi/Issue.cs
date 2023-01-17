using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

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

    [DataContract]
    public class Issue : IReadOnlyIssue
    {

        public Issue()
            : this(null)
        {
        }

        private Issue(Issue original)
        {
            m_data = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (original != null)
            {
                this.Type = original.Type;
                this.Category = original.Category;
                this.Message = original.Message;
                this.IsInfrastructureIssue = original.IsInfrastructureIssue;
                this.Data.AddRange(original.Data);
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
