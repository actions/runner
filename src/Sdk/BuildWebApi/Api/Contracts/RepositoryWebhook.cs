using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a repository's webhook returned from a source provider.
    /// </summary>
    [DataContract]
    public class RepositoryWebhook
    {
        /// <summary>
        ///  The types of triggers the webhook was created for.
        /// </summary>
        public List<DefinitionTriggerType> Types
        {
            get
            {
                if (m_types == null)
                {
                    m_types = new List<DefinitionTriggerType>();
                }

                return m_types;
            }
            set
            {
                m_types = value;
            }
        }

        /// <summary>
        /// The friendly name of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        /// <summary>
        /// The URL of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Url { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedTypes, ref m_types, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_types, ref m_serializedTypes);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedTypes = null;
        }

        [DataMember(Name = nameof(Types), EmitDefaultValue = false)]
        private List<DefinitionTriggerType> m_serializedTypes;

        private List<DefinitionTriggerType> m_types;
    }
}
