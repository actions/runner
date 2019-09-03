using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class AgentJobRequestMessage
    {
        [JsonConstructor]
        internal AgentJobRequestMessage()
        {
        }

        /// <summary>
        /// Job request message sent to the runner
        /// </summary>
        /// <param name="environmentVariables">Hierarchy of environment variables to overlay, last wins.</param>
        public AgentJobRequestMessage(
            TaskOrchestrationPlanReference plan,
            TimelineReference timeline,
            Guid jobId,
            String jobDisplayName,
            String jobName,
            String jobContainer,
            IDictionary<String, String> jobSidecarContainers,
            IList<TemplateToken> environmentVariables,
            IDictionary<String, VariableValue> variables,
            IList<MaskHint> maskHints,
            JobResources jobResources,
            DictionaryContextData contextData,
            WorkspaceOptions workspaceOptions,
            IEnumerable<JobStep> steps,
            IEnumerable<ContextScope> scopes)
        {
            this.MessageType = JobRequestMessageTypes.PipelineAgentJobRequest;
            this.Plan = plan;
            this.JobId = jobId;
            this.JobDisplayName = jobDisplayName;
            this.JobName = jobName;
            this.JobContainer = jobContainer;
            this.Timeline = timeline;
            this.Resources = jobResources;
            this.Workspace = workspaceOptions;

            m_variables = new Dictionary<String, VariableValue>(variables, StringComparer.OrdinalIgnoreCase);
            m_maskHints = new List<MaskHint>(maskHints);
            m_steps = new List<JobStep>(steps);

            if (scopes != null)
            {
                m_scopes = new List<ContextScope>(scopes);
            }

            if (jobSidecarContainers?.Count > 0)
            {
                m_jobSidecarContainers = new Dictionary<String, String>(jobSidecarContainers, StringComparer.OrdinalIgnoreCase);
            }

            if (environmentVariables?.Count > 0)
            {
                m_environmentVariables = new List<TemplateToken>(environmentVariables);
            }

            this.ContextData = new Dictionary<String, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            if (contextData?.Count > 0)
            {
                foreach (var pair in contextData)
                {
                    this.ContextData[pair.Key] = pair.Value;
                }
            }
        }

        [DataMember]
        public String MessageType
        {
            get;
            private set;
        }

        [DataMember]
        public TaskOrchestrationPlanReference Plan
        {
            get;
            private set;
        }

        [DataMember]
        public TimelineReference Timeline
        {
            get;
            private set;
        }

        [DataMember]
        public Guid JobId
        {
            get;
            private set;
        }

        [DataMember]
        public String JobDisplayName
        {
            get;
            private set;
        }

        [DataMember]
        public String JobName
        {
            get;
            private set;
        }

        [DataMember]
        public String JobContainer
        {
            get;
            private set;
        }

        [DataMember]
        public Int64 RequestId
        {
            get;
            internal set;
        }

        [DataMember]
        public DateTime LockedUntil
        {
            get;
            internal set;
        }

        [DataMember]
        public JobResources Resources
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IDictionary<String, PipelineContextData> ContextData
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public WorkspaceOptions Workspace
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of mask hints
        /// </summary>
        public List<MaskHint> MaskHints
        {
            get
            {
                if (m_maskHints == null)
                {
                    m_maskHints = new List<MaskHint>();
                }
                return m_maskHints;
            }
        }

        /// <summary>
        /// Gets the hierarchy of environment variables to overlay, last wins.
        /// </summary>
        public IList<TemplateToken> EnvironmentVariables
        {
            get
            {
                if (m_environmentVariables == null)
                {
                    m_environmentVariables = new List<TemplateToken>();
                }
                return m_environmentVariables;
            }
        }

        /// <summary>
        /// Gets the collection of variables associated with the current context.
        /// </summary>
        public IDictionary<String, VariableValue> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
                }
                return m_variables;
            }
        }

        public IList<JobStep> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<JobStep>();
                }
                return m_steps;
            }
        }

        public IList<ContextScope> Scopes
        {
            get
            {
                if (m_scopes == null)
                {
                    m_scopes = new List<ContextScope>();
                }
                return m_scopes;
            }
        }

        public IDictionary<String, String> JobSidecarContainers
        {
            get
            {
                if (m_jobSidecarContainers == null)
                {
                    m_jobSidecarContainers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_jobSidecarContainers;
            }
        }

        public TaskAgentMessage GetAgentMessage()
        {
            var body = JsonUtility.ToString(this);

            return new TaskAgentMessage
            {
                Body = body,
                MessageType = JobRequestMessageTypes.PipelineAgentJobRequest
            };
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_environmentVariables?.Count == 0)
            {
                m_environmentVariables = null;
            }

            if (m_maskHints?.Count == 0)
            {
                m_maskHints = null;
            }
            else
            {
                m_maskHints = new List<MaskHint>(this.m_maskHints.Distinct());
            }

            if (m_scopes?.Count == 0)
            {
                m_scopes = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }

            if (m_jobSidecarContainers?.Count == 0)
            {
                m_jobSidecarContainers = null;
            }
        }

        [DataMember(Name = "EnvironmentVariables", EmitDefaultValue = false)]
        private List<TemplateToken> m_environmentVariables;

        [DataMember(Name = "Mask", EmitDefaultValue = false)]
        private List<MaskHint> m_maskHints;

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<JobStep> m_steps;

        [DataMember(Name = "Scopes", EmitDefaultValue = false)]
        private List<ContextScope> m_scopes;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, VariableValue> m_variables;

        [DataMember(Name = "JobSidecarContainers", EmitDefaultValue = false)]
        private IDictionary<String, String> m_jobSidecarContainers;
    }
}
