using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Represents an option that may affect the way an agent runs the job.
    /// </summary>
    [DataContract]
    public class JobOption : ICloneable
    {
        /// <summary>
        /// Initializes a new <c>JobOption</c> with an empty collection of data.
        /// </summary>
        public JobOption()
        {
        }

        private JobOption(JobOption optionToClone)
        {
            this.Id = optionToClone.Id;

            if (optionToClone.m_data != null)
            {
                m_data = new Dictionary<String, String>(optionToClone.m_data, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the id of the option.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the data associated with the option.
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_serializedData != null && m_serializedData.Count > 0)
            {
                m_data = new Dictionary<String, String>(m_serializedData, StringComparer.OrdinalIgnoreCase);
            }

            m_serializedData = null;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            m_serializedData = this.Data.Count > 0 ? this.Data : null;
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        /// <summary>
        /// Creates a deep copy of the job option.
        /// </summary>
        /// <returns>A deep copy of the job option</returns>
        public JobOption Clone()
        {
            return new JobOption(this);
        }

        private Dictionary<String, String> m_data;

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedData;
    }
}
