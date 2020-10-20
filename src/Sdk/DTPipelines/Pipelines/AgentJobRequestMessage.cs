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
            TemplateToken jobContainer,
            TemplateToken jobServiceContainers,
            IList<TemplateToken> environmentVariables,
            IDictionary<String, VariableValue> variables,
            IList<MaskHint> maskHints,
            JobResources jobResources,
            DictionaryContextData contextData,
            WorkspaceOptions workspaceOptions,
            IEnumerable<JobStep> steps,
            IList<String> fileTable,
            TemplateToken jobOutputs,
            IList<TemplateToken> defaults,
            ActionsEnvironmentReference actionsEnvironment)
        {
            this.MessageType = JobRequestMessageTypes.PipelineAgentJobRequest;
            this.Plan = plan;
            this.JobId = jobId;
            this.JobDisplayName = jobDisplayName;
            this.JobName = jobName;
            this.JobContainer = jobContainer;
            this.JobServiceContainers = jobServiceContainers;
            this.Timeline = timeline;
            this.Resources = jobResources;
            this.Workspace = workspaceOptions;
            this.JobOutputs = jobOutputs;
            this.ActionsEnvironment = actionsEnvironment;
            m_variables = new Dictionary<String, VariableValue>(variables, StringComparer.OrdinalIgnoreCase);
            m_maskHints = new List<MaskHint>(maskHints);
            m_steps = new List<JobStep>(steps);

            if (environmentVariables?.Count > 0)
            {
                m_environmentVariables = new List<TemplateToken>(environmentVariables);
            }

            if (defaults?.Count > 0)
            {
                m_defaults = new List<TemplateToken>(defaults);
            }

            this.ContextData = new Dictionary<String, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            if (contextData?.Count > 0)
            {
                foreach (var pair in contextData)
                {
                    this.ContextData[pair.Key] = pair.Value;
                }
            }

            if (fileTable?.Count > 0)
            {
                m_fileTable = new List<String>(fileTable);
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

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobContainer
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobServiceContainers
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobOutputs
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
        /// Gets the hierarchy of defaults to overlay, last wins.
        /// </summary>
        public IList<TemplateToken> Defaults
        {
            get
            {
                if (m_defaults == null)
                {
                    m_defaults = new List<TemplateToken>();
                }
                return m_defaults;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public ActionsEnvironmentReference ActionsEnvironment
        {
            get;
            set;
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

        /// <summary>
        /// Gets the table of files used when parsing the pipeline (e.g. yaml files)
        /// </summary>
        public IList<String> FileTable
        {
            get
            {
                if (m_fileTable == null)
                {
                    m_fileTable = new List<String>();
                }
                return m_fileTable;
            }
        }

        // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
        public void SetJobSidecarContainers(IDictionary<String, String> value)
        {
            m_jobSidecarContainers = value;
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

        // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
        internal static TemplateToken ConvertToTemplateToken(ContainerResource resource)
        {
            var result = new MappingToken(null, null, null);

            var image = resource.Image;
            if (!string.IsNullOrEmpty(image))
            {
                result.Add(new StringToken(null, null, null, "image"), new StringToken(null, null, null, image));
            }

            var options = resource.Options;
            if (!string.IsNullOrEmpty(options))
            {
                result.Add(new StringToken(null, null, null, "options"), new StringToken(null, null, null, options));
            }

            var environment = resource.Environment;
            if (environment?.Count > 0)
            {
                var mapping = new MappingToken(null, null, null);
                foreach (var pair in environment)
                {
                    mapping.Add(new StringToken(null, null, null, pair.Key), new StringToken(null, null, null, pair.Value));
                }
                result.Add(new StringToken(null, null, null, "env"), mapping);
            }

            var ports = resource.Ports;
            if (ports?.Count > 0)
            {
                var sequence = new SequenceToken(null, null, null);
                foreach (var item in ports)
                {
                    sequence.Add(new StringToken(null, null, null, item));
                }
                result.Add(new StringToken(null, null, null, "ports"), sequence);
            }

            var volumes = resource.Volumes;
            if (volumes?.Count > 0)
            {
                var sequence = new SequenceToken(null, null, null);
                foreach (var item in volumes)
                {
                    sequence.Add(new StringToken(null, null, null, item));
                }
                result.Add(new StringToken(null, null, null, "volumes"), sequence);
            }

            return result;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
            if (JobContainer is StringToken jobContainerStringToken)
            {
                var resourceAlias = jobContainerStringToken.Value;
                var resource = Resources?.Containers.SingleOrDefault(x => string.Equals(x.Alias, resourceAlias, StringComparison.OrdinalIgnoreCase));
                if (resource != null)
                {
                    JobContainer = ConvertToTemplateToken(resource);
                    m_jobContainerResourceAlias = resourceAlias;
                }
            }

            // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
            if (m_jobSidecarContainers?.Count > 0 && (JobServiceContainers == null || JobServiceContainers.Type == TokenType.Null))
            {
                var services = new MappingToken(null, null, null);
                foreach (var pair in m_jobSidecarContainers)
                {
                    var networkAlias = pair.Key;
                    var serviceResourceAlias = pair.Value;
                    var serviceResource = Resources.Containers.Single(x => string.Equals(x.Alias, serviceResourceAlias, StringComparison.OrdinalIgnoreCase));
                    services.Add(new StringToken(null, null, null, networkAlias), ConvertToTemplateToken(serviceResource));
                }
                JobServiceContainers = services;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_environmentVariables?.Count == 0)
            {
                m_environmentVariables = null;
            }

            if (m_defaults?.Count == 0)
            {
                m_defaults = null;
            }

            if (m_fileTable?.Count == 0)
            {
                m_fileTable = null;
            }

            if (m_maskHints?.Count == 0)
            {
                m_maskHints = null;
            }
            else if (m_maskHints != null)
            {
                m_maskHints = new List<MaskHint>(this.m_maskHints.Distinct());
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }

            // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
            if (!string.IsNullOrEmpty(m_jobContainerResourceAlias))
            {
                JobContainer = new StringToken(null, null, null, m_jobContainerResourceAlias);
            }
        }

        [DataMember(Name = "EnvironmentVariables", EmitDefaultValue = false)]
        private List<TemplateToken> m_environmentVariables;

        [DataMember(Name = "Defaults", EmitDefaultValue = false)]
        private List<TemplateToken> m_defaults;

        [DataMember(Name = "FileTable", EmitDefaultValue = false)]
        private List<String> m_fileTable;

        [DataMember(Name = "Mask", EmitDefaultValue = false)]
        private List<MaskHint> m_maskHints;

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<JobStep> m_steps;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, VariableValue> m_variables;

        // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
        [DataMember(Name = "JobSidecarContainers", EmitDefaultValue = false)]
        private IDictionary<String, String> m_jobSidecarContainers;

        // todo: remove after feature-flag DistributedTask.EvaluateContainerOnRunner is enabled everywhere
        [IgnoreDataMember]
        private string m_jobContainerResourceAlias;
    }
}
