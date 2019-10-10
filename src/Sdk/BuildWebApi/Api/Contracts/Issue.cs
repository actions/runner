using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an issue (error, warning) associated with a build.
    /// </summary>
    [DataContract]
    public class Issue : BaseSecuredObject
    {
        public Issue()
        {
        }

        internal Issue(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        private Issue(
            Issue issueToBeCloned)
            : base(issueToBeCloned)
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

        /// <summary>
        /// The type (error, warning) of the issue.
        /// </summary>
        [DataMember(Order = 1)]
        public IssueType Type
        {
            get;
            set;
        }

        /// <summary>
        /// The category.
        /// </summary>
        [DataMember(Order = 2)]
        public String Category
        {
            get;
            set;
        }

        /// <summary>
        /// A description of the issue.
        /// </summary>
        [DataMember(Order = 3)]
        public String Message
        {
            get;
            set;
        }

        /// <summary>
        /// A dictionary containing details about the issue.
        /// </summary>
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

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public Issue Clone()
        {
            return new Issue(this);
        }

        [DataMember(Name = "Data", EmitDefaultValue = false, Order = 4)]
        private IDictionary<String, String> m_data;
    }
}
