using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a trigger for a buld definition.
    /// </summary>
    [DataContract]
    [KnownType(typeof(ContinuousIntegrationTrigger))]
    [KnownType(typeof(GatedCheckInTrigger))]
    [KnownType(typeof(ScheduleTrigger))]
    [KnownType(typeof(PullRequestTrigger))]
    [JsonConverter(typeof(BuildTriggerJsonConverter))]
    public abstract class BuildTrigger : BaseSecuredObject
    {
        protected BuildTrigger(DefinitionTriggerType triggerType)
            : this(triggerType, null)
        {
        }

        protected internal BuildTrigger(
            DefinitionTriggerType triggerType,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.TriggerType = triggerType;
        }

        /// <summary>
        /// The type of the trigger.
        /// </summary>
        [DataMember]
        public DefinitionTriggerType TriggerType
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Represents a continuous integration (CI) trigger.
    /// </summary>
    [DataContract]
    public sealed class ContinuousIntegrationTrigger : BuildTrigger
    {
        public ContinuousIntegrationTrigger()
            : this(null)
        {
        }

        internal ContinuousIntegrationTrigger(
            ISecuredObject securedObject)
            : base(DefinitionTriggerType.ContinuousIntegration, securedObject)
        {
            MaxConcurrentBuildsPerBranch = 1;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 SettingsSourceType
        {
            get
            {
                if (m_settingsSourceType == 0)
                {
                    m_settingsSourceType = WebApi.SettingsSourceType.Definition;
                }

                return m_settingsSourceType;
            }
            set
            {
                m_settingsSourceType = value;
            }
        }

        /// <summary>
        /// Indicates whether changes should be batched while another CI build is running.
        /// </summary>
        /// <remarks>
        /// If this is true, then changes submitted while a CI build is running will be batched and built in one new CI build when the current build finishes.
        /// If this is false, then a new CI build will be triggered for each change to the repository.
        /// </remarks>
        [DataMember]
        public Boolean BatchChanges
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum number of simultaneous CI builds that will run per branch.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 MaxConcurrentBuildsPerBranch
        {
            get;
            set;
        }

        /// <summary>
        /// A list of filters that describe which branches will trigger builds.
        /// </summary>
        public List<String> BranchFilters
        {
            get
            {
                if (m_branchFilters == null)
                {
                    m_branchFilters = new List<String>();
                }

                return m_branchFilters;
            }
            internal set
            {
                m_branchFilters = value;
            }
        }

        // added in 3.0
        /// <summary>
        /// A list of filters that describe which paths will trigger builds.
        /// </summary>
        public List<String> PathFilters
        {
            get
            {
                if (m_pathFilters == null)
                {
                    m_pathFilters = new List<String>();
                }

                return m_pathFilters;
            }
            internal set
            {
                m_pathFilters = value;
            }
        }

        /// <summary>
        /// The polling interval, in seconds.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32? PollingInterval
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the job used to poll an external repository.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid PollingJobId
        {
            // This is the ID of the polling job that polls the external repository.
            // Once the build definition is saved/updated, this value is set.
            get;
            set;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_settingsSourceType == WebApi.SettingsSourceType.Definition)
            {
                m_settingsSourceType = 0;
            }
        }

        [DataMember(Name = "BranchFilters", EmitDefaultValue = false)]
        private List<String> m_branchFilters;

        [DataMember(Name = "PathFilters", EmitDefaultValue = false)]
        private List<String> m_pathFilters;

        [DataMember(Name = "SettingsSourceType", EmitDefaultValue = false)]
        private Int32 m_settingsSourceType;
    }

    /// <summary>
    /// Represents a gated check-in trigger.
    /// </summary>
    [DataContract]
    public sealed class GatedCheckInTrigger : BuildTrigger
    {
        public GatedCheckInTrigger()
            : this(null)
        {
        }

        internal GatedCheckInTrigger(
            ISecuredObject securedObject)
            : base(DefinitionTriggerType.GatedCheckIn, securedObject)
        {
        }

        /// <summary>
        /// Indicates whether CI triggers should run after the gated check-in succeeds.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean RunContinuousIntegration
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether to take workspace mappings into account when determining whether a build should run.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean UseWorkspaceMappings
        {
            get;
            set;
        }

        /// <summary>
        /// A list of filters that describe which paths will trigger builds.
        /// </summary>
        public List<String> PathFilters
        {
            get
            {
                if (m_pathFilters == null)
                {
                    m_pathFilters = new List<String>();
                }

                return m_pathFilters;
            }
            internal set
            {
                m_pathFilters = value;
            }
        }

        [DataMember(Name = "PathFilters", EmitDefaultValue = false)]
        private List<String> m_pathFilters;
    }

    /// <summary>
    /// Represents a schedule trigger.
    /// </summary>
    [DataContract]
    public sealed class ScheduleTrigger : BuildTrigger
    {
        public ScheduleTrigger()
            : this(null)
        {
        }

        internal ScheduleTrigger(
            ISecuredObject securedObject)
            : base(DefinitionTriggerType.Schedule, securedObject)
        {
        }

        /// <summary>
        /// A list of schedule entries that describe when builds should run.
        /// </summary>
        public List<Schedule> Schedules
        {
            get
            {
                if (m_schedules == null)
                {
                    m_schedules = new List<Schedule>();
                }

                return m_schedules;
            }
            set
            {
                m_schedules = value;
            }
        }

        [DataMember(Name = "Schedules", EmitDefaultValue = false)]
        private List<Schedule> m_schedules;
    }

    /// <summary>
    /// Represents a pull request trigger.
    /// </summary>
    [DataContract]
    public class PullRequestTrigger : BuildTrigger
    {
        public PullRequestTrigger()
            : this(null)
        {
        }

        internal PullRequestTrigger(
            ISecuredObject securedObject)
            : base(DefinitionTriggerType.PullRequest, securedObject)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 SettingsSourceType
        {
            get
            {
                if (m_settingsSourceType == 0)
                {
                    m_settingsSourceType = WebApi.SettingsSourceType.Definition;
                }

                return m_settingsSourceType;
            }
            set
            {
                m_settingsSourceType = value;
            }
        }


        /// <summary>
        /// Describes if forks of a selected repository should build and use secrets.
        /// </summary>
        public Forks Forks
        {
            get
            {
                if (m_forks == null)
                {
                    m_forks = new Forks();
                }
                return m_forks;
            }
            set
            {
                m_forks = value;
            }
        }

        /// <summary>
        /// A list of filters that describe which branches will trigger builds.
        /// </summary>
        public List<String> BranchFilters
        {
            get
            {
                if (m_branchFilters == null)
                {
                    m_branchFilters = new List<String>();
                }

                return m_branchFilters;
            }
            set
            {
                m_branchFilters = value;
            }
        }

        /// <summary>
        /// A list of filters that describe which paths will trigger builds.
        /// </summary>
        public List<String> PathFilters
        {
            get
            {
                if (m_pathFilters == null)
                {
                    m_pathFilters = new List<String>();
                }

                return m_pathFilters;
            }
            set
            {
                m_pathFilters = value;
            }
        }

        /// <summary>
        /// Indicates if an update to a PR should delete current in-progress builds.
        /// </summary>
        [DataMember(Name = "AutoCancel", EmitDefaultValue = false)]
        public Boolean? AutoCancel { get; set; }

        [DataMember(Name = "RequireCommentsForNonTeamMembersOnly")]
        public Boolean RequireCommentsForNonTeamMembersOnly { get; set; }

        [DataMember(Name = "IsCommentRequiredForPullRequest")]
        public Boolean IsCommentRequiredForPullRequest { get; set; }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_settingsSourceType == WebApi.SettingsSourceType.Definition)
            {
                m_settingsSourceType = 0;
            }
        }

        [DataMember(Name = "SettingsSourceType", EmitDefaultValue = false)]
        private Int32 m_settingsSourceType;

        [DataMember(Name = "BranchFilters", EmitDefaultValue = false)]
        private List<String> m_branchFilters;

        [DataMember(Name = "Forks", EmitDefaultValue = false)]
        private Forks m_forks;

        [DataMember(Name = "PathFilters", EmitDefaultValue = false)]
        private List<String> m_pathFilters;
    }

    /// <summary>
    /// Represents a build completion trigger. 
    /// </summary>
    [DataContract]
    public class BuildCompletionTrigger : BuildTrigger
    {
        public BuildCompletionTrigger()
            : this(null)
        {
        }
        public BuildCompletionTrigger(
            ISecuredObject securedObject)
            : base(DefinitionTriggerType.BuildCompletion, securedObject)
        {
        }

        /// <summary>
        /// A reference to the definition that should trigger builds for this definition.
        /// </summary>
        [DataMember]
        public DefinitionReference Definition { get; set; }

        [DataMember]
        public Boolean RequiresSuccessfulBuild { get; set; }

        [DataMember]
        public List<String> BranchFilters { get; set; }
    }
}
