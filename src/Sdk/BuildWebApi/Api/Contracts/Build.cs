using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using GitHub.Core.WebApi;
//using GitHub.Core.WebApi;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Data representation of a build.
    /// </summary>
    [DataContract]
    public class Build : ISecuredObject
    {
        public Build()
        {
            Reason = BuildReason.Manual;
            Priority = QueuePriority.Normal;
        }

        #region BuildReference members
        // these are also present in BuildReference.  ideally this class would inherit from that.
        // however, moving them to a base class changes the order in which they are serialized to xml
        // which breaks compat with subscribers (like RM) who may not be on the same milestone
        // TODO: remove these when we figure out how to version service bus events

        /// <summary>
        /// The ID of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [Key]
        public Int32 Id
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The build number/name of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String BuildNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The status of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// The build result.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was queued.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? QueueTime
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was started.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was completed.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        /// <summary>
        /// The links to other objects related to this object.
        /// </summary>
        public ReferenceLinks Links
        {
            get
            {
                if (m_links == null)
                {
                    m_links = new ReferenceLinks();
                }
                return m_links;
            }
        }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        private ReferenceLinks m_links;

        #endregion

        /// <summary>
        /// The REST URL of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Url
        {
            get;
            set;
        }

        /// <summary>
        /// The definition associated with the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DefinitionReference Definition
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The build number revision.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? BuildNumberRevision
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The team project.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TeamProjectReference Project
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The URI of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Uri
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The source branch.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String SourceBranch
        {
            get;
            set;
        }

        /// <summary>
        /// The source version.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String SourceVersion
        {
            get;
            set;
        }

        /// <summary>
        /// The queue. This is only set if the definition type is Build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentPoolQueue Queue
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The agent specification for the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentSpecification AgentSpecification
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The current position of the build in the queue.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? QueuePosition
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The build's priority.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public QueuePriority Priority
        {
            get;
            set;
        }

        /// <summary>
        /// The reason that the build was created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildReason Reason
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The identity on whose behalf the build was queued.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef RequestedFor
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The identity that queued the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef RequestedBy
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The date the build was last changed.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime LastChangedDate
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The identity representing the process or person that last changed the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef LastChangedBy
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The date the build was deleted.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DeletedDate
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The identity of the process or person that deleted the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef DeletedBy
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The description of how the build was deleted.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DeletedReason
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The parameters for the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// A list of demands that represents the agent capabilities required by this build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<Demand> Demands
        {
            get;
            set;
        }

        /// <summary>
        /// The orchestration plan for the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationPlanReference OrchestrationPlan
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The list of Orchestration plans associated with the build.
        /// </summary>
        /// <remarks>
        /// The build may have plans in addition to the main plan. For example, the cleanup job may have an orchestration plan associated with it.
        /// </remarks>
        public List<TaskOrchestrationPlanReference> Plans
        {
            get
            {
                if (m_plans == null)
                {
                    m_plans = new List<TaskOrchestrationPlanReference>();
                }

                return m_plans;
            }
            set
            {
                m_plans = value;
            }
        }

        /// <summary>
        /// Information about the build logs.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildLogReference Logs
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildRepository Repository
        {
            get;
            set;
        }

        /// <summary>
        /// Additional options for queueing the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public QueueOptions QueueOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the build has been deleted.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Deleted
        {
            get;
            set;
        }

        /// <summary>
        /// A collection of properties which may be used to extend the storage fields available
        /// for a given build.
        /// </summary>
        public PropertiesCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new PropertiesCollection();
                }
                return m_properties;
            }
            internal set
            {
                m_properties = value;
            }
        }

        /// <summary>
        /// A collection of tags associated with the build.
        /// </summary>
        public List<String> Tags
        {
            get
            {
                if (m_tags == null)
                {
                    m_tags = new List<String>();
                }

                return m_tags;
            }
            internal set
            {
                m_tags = value;
            }
        }

        /// <summary>
        /// The list of validation errors and warnings.
        /// </summary>
        public List<BuildRequestValidationResult> ValidationResults
        {
            get
            {
                if (m_validationResults == null)
                {
                    m_validationResults = new List<BuildRequestValidationResult>();
                }
                return m_validationResults;
            }
        }

        /// <summary>
        /// Indicates whether the build should be skipped by retention policies.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean? KeepForever
        {
            get;
            set;
        }

        /// <summary>
        /// The quality of the xaml build (good, bad, etc.)
        /// </summary>
        /// <remarks>
        /// This is only used for XAML builds.
        /// </remarks>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Quality
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the build is retained by a release.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean? RetainedByRelease
        {
            get;
            set;
        }

        /// <summary>
        /// The build that triggered this build via a Build completion trigger.
        /// </summary>
        [DataMember]
        public Build TriggeredByBuild { get; set; }

        /// <summary>
        /// Trigger-specific information about the build.
        /// </summary>
        public IDictionary<String, String> TriggerInfo
        {
            get
            {
                if (m_triggerInfo == null)
                {
                    m_triggerInfo = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_triggerInfo;
            }
            internal set
            {
                if (value != null)
                {
                    m_triggerInfo = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;

        [DataMember(EmitDefaultValue = false, Name = "Tags")]
        private List<String> m_tags;

        [DataMember(EmitDefaultValue = false, Name = "ValidationResults")]
        private List<BuildRequestValidationResult> m_validationResults;

        /// <summary>
        /// Orchestration plans associated with the build (build, cleanup)
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Plans")]
        private List<TaskOrchestrationPlanReference> m_plans;

        /// <summary>
        /// Sourceprovider-specific information about what triggered the build
        /// </summary>
        /// <remarks>Added in 3.2-preview.3</remarks>
        [DataMember(EmitDefaultValue = false, Name = "TriggerInfo")]
        private Dictionary<String, String> m_triggerInfo;

        #region ISecuredObject implementation

        Guid ISecuredObject.NamespaceId => Security.BuildNamespaceId;

        Int32 ISecuredObject.RequiredPermissions => BuildPermissions.ViewBuilds;

        String ISecuredObject.GetToken()
        {
            if (!String.IsNullOrEmpty(m_nestingToken))
            {
                return m_nestingToken;
            }

            return ((ISecuredObject)this.Definition)?.GetToken();
        }

        internal void SetNestingSecurityToken(String tokenValue)
        {
            // Spike: investigate imposing restrictions on the amount of information being returned 
            // when a nesting security token is being used.
            m_nestingToken = tokenValue;
        }

        private String m_nestingToken = String.Empty;
        #endregion
    }
}
