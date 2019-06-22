using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// For back-compat with extensions that use the old Steps format instead of Process and Phases
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildDefinitionReference3_2 : DefinitionReference
    {
        public BuildDefinitionReference3_2()
        {
            Type = DefinitionType.Build;
            QueueStatus = DefinitionQueueStatus.Enabled;
        }

        /// <summary>
        /// The quality of the definition document (draft, etc.)
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Quality")]
        public DefinitionQuality? DefinitionQuality
        {
            get;
            set;
        }

        /// <summary>
        /// The author of the definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef AuthoredBy
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// A reference to the definition that this definition is a draft of, if this is a draft definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "draftOf")]
        public DefinitionReference ParentDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// The list of drafts associated with this definition, if this is not a draft definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<DefinitionReference> Drafts
        {
            get
            {
                return m_drafts ?? (m_drafts = new List<DefinitionReference>());
            }
            internal set
            {
                m_drafts = value;
            }
        }

        /// <summary>
        /// The default queue for builds run against this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentPoolQueue Queue
        {
            get;
            set;
        }

        /// <summary>
        /// The metrics for this definition.
        /// </summary>
        public List<BuildMetric> Metrics
        {
            get
            {
                return m_metrics ?? (m_metrics = new List<BuildMetric>());
            }
            internal set
            {
                m_metrics = value;
            }
        }

        /// <summary>
        /// The links to other objects related to this object.
        /// </summary>
        public ReferenceLinks Links => m_links ?? (m_links = new ReferenceLinks());

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedMetrics, ref m_metrics, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_metrics, ref m_serializedMetrics);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedMetrics = null;
        }

        [DataMember(Name = "Metrics", EmitDefaultValue = false)]
        private List<BuildMetric> m_serializedMetrics;

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        private ReferenceLinks m_links;

        private List<BuildMetric> m_metrics;

        private List<DefinitionReference> m_drafts;
    }
}
