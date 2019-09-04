using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Job
    {
        [JsonConstructor]
        public Job()
        {
        }

        private Job(Job jobToCopy)
        {
            this.Id = jobToCopy.Id;
            this.Name = jobToCopy.Name;
            this.DisplayName = jobToCopy.DisplayName;
            this.Container = jobToCopy.Container?.Clone();
            this.ServiceContainers = jobToCopy.ServiceContainers?.Clone();
            this.ContinueOnError = jobToCopy.ContinueOnError;
            this.TimeoutInMinutes = jobToCopy.TimeoutInMinutes;
            this.CancelTimeoutInMinutes = jobToCopy.CancelTimeoutInMinutes;
            this.Workspace = jobToCopy.Workspace?.Clone();
            this.Target = jobToCopy.Target?.Clone();
            this.EnvironmentVariables = jobToCopy.EnvironmentVariables?.Clone();

            if (jobToCopy.m_demands != null && jobToCopy.m_demands.Count > 0)
            {
                m_demands = new List<Demand>(jobToCopy.m_demands.Select(x => x.Clone()));
            }

            if (jobToCopy.m_steps != null && jobToCopy.m_steps.Count > 0)
            {
                m_steps = new List<JobStep>(jobToCopy.m_steps.Select(x => x.Clone() as JobStep));
            }

            if (jobToCopy.m_variables != null && jobToCopy.m_variables.Count > 0)
            {
                m_variables = new List<IVariable>(jobToCopy.m_variables);
            }

            if (jobToCopy.m_sidecarContainers != null && jobToCopy.m_sidecarContainers.Count > 0)
            {
                m_sidecarContainers = new Dictionary<String, String>(jobToCopy.m_sidecarContainers, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Container
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken ServiceContainers
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken EnvironmentVariables
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 TimeoutInMinutes
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 CancelTimeoutInMinutes
        {
            get;
            set;
        }

        public IList<Demand> Demands
        {
            get
            {
                if (m_demands == null)
                {
                    m_demands = new List<Demand>();
                }
                return m_demands;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IdentityRef ExecuteAs
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public WorkspaceOptions Workspace
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public PhaseTarget Target
        {
            get;
            set;
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

        public IDictionary<String, String> SidecarContainers
        {
            get
            {
                if (m_sidecarContainers == null)
                {
                    m_sidecarContainers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_sidecarContainers;
            }
        }

        public IList<IVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new List<IVariable>();
                }
                return m_variables;
            }
        }

        public Job Clone()
        {
            return new Job(this);
        }

        /// <summary>
        /// Creates an instance of a task using the specified execution context.
        /// </summary>
        /// <param name="context">The job execution context</param>
        /// <param name="taskName">The name of the task in the steps list</param>
        /// <returns></returns>
        public CreateTaskResult CreateTask(
            JobExecutionContext context,
            String taskName)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(taskName, nameof(taskName));

            TaskDefinition definition = null;
            var task = this.Steps.SingleOrDefault(x => taskName.Equals(x.Name, StringComparison.OrdinalIgnoreCase))?.Clone() as TaskStep;
            if (task != null)
            {
                definition = context.TaskStore.ResolveTask(task.Reference.Id, task.Reference.Version);
                foreach (var input in definition.Inputs.Where(x => x != null))
                {
                    var key = input.Name?.Trim() ?? String.Empty;
                    if (!String.IsNullOrEmpty(key))
                    {
                        if (!task.Inputs.ContainsKey(key))
                        {
                            task.Inputs[key] = input.DefaultValue?.Trim() ?? String.Empty;
                        }
                    }
                }

                // Now expand any macros which appear in inputs
                foreach (var input in task.Inputs.ToArray())
                {
                    task.Inputs[input.Key] = context.ExpandVariables(input.Value);
                }

                // Set the system variables populated while running an individual task
                context.Variables[WellKnownDistributedTaskVariables.TaskInstanceId] = task.Id.ToString("D");
                context.Variables[WellKnownDistributedTaskVariables.TaskDisplayName] = task.DisplayName ?? task.Name;
                context.Variables[WellKnownDistributedTaskVariables.TaskInstanceName] = task.Name;
            }

            return new CreateTaskResult(task, definition);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_demands?.Count == 0)
            {
                m_demands = null;
            }

            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }

            if (m_scopes?.Count == 0)
            {
                m_scopes = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }
        }

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private List<Demand> m_demands;

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<JobStep> m_steps;

        [DataMember(Name = "Scopes", EmitDefaultValue = false)]
        private List<ContextScope> m_scopes;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private List<IVariable> m_variables;

        [DataMember(Name = "SidecarContainers", EmitDefaultValue = false)]
        private IDictionary<String, String> m_sidecarContainers;
    }
}
