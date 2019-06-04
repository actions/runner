using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationPlan : TaskOrchestrationPlanReference
    {
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        [DataMember]
        public TaskOrchestrationPlanState State
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskResult? Result
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ResultCode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TimelineReference Timeline
        {
            get;
            set;
        }

        public PlanEnvironment Environment
        {
            get
            {
                return m_environment;
            }
            set
            {
                m_environment = value;
                m_processEnvironment = value;
            }
        }

        public TaskOrchestrationContainer Implementation
        {
            get
            {
                return m_implementation;
            }
            set
            {
                m_process = value;
                m_implementation = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IOrchestrationProcess Process
        {
            get
            {
                return m_process;
            }
            set
            {
                m_process = value;
                m_implementation = value as TaskOrchestrationContainer;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IOrchestrationEnvironment ProcessEnvironment
        {
            get
            {
                return m_processEnvironment;
            }
            set
            {
                m_processEnvironment = value;
                m_environment = value as PlanEnvironment;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid RequestedById
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid RequestedForId
        {
            get;
            set;
        }

        internal PlanTemplateType TemplateType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskLogReference InitializationLog
        {
            get;
            set;
        }

        // Currently these members are not serialized on the wire since that would technically be an API break for 
        // the 1.0 version. While additive, existing clients wouldn't understand it and could blow up. Until this
        // public model is finalized we will not send this data over the wire and will not revision the API.
        private IOrchestrationProcess m_process;
        private IOrchestrationEnvironment m_processEnvironment;

        [DataMember(Name = "Environment", EmitDefaultValue = false)]
        private PlanEnvironment m_environment;

        [DataMember(Name = "Implementation", EmitDefaultValue = false)]
        private TaskOrchestrationContainer m_implementation;
    }
}
