using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a build definition.
    /// </summary>
    [DataContract]
    public class BuildDefinitionReference : DefinitionReference
    {
        public BuildDefinitionReference()
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
        /// The latest build for this definition.
        /// </summary>
        public Build LatestBuild
        {
            get
            {
                return m_latestBuild;
            }
            internal set
            {
                m_latestBuild = value;
            }
        }

        /// <summary>
        /// The latest completed build for this definition.
        /// </summary>
        public Build LatestCompletedBuild
        {
            get
            {
                return m_latestCompletedBuild;
            }
            internal set
            {
                m_latestCompletedBuild = value;
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

        [DataMember(EmitDefaultValue = false, Name = "LatestBuild")]
        private Build m_latestBuild;

        [DataMember(EmitDefaultValue = false, Name = "LatestCompletedBuild")]
        private Build m_latestCompletedBuild;
    }
}
