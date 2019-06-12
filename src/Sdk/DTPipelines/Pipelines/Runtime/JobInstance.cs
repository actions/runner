using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public sealed class JobInstance
    {
        public JobInstance()
            : this(String.Empty)
        {
        }

        public JobInstance(String name)
            : this(name, 1)
        {
        }

        public JobInstance(
            String name,
            Int32 attempt)
        {
            this.Name = name;
            this.Attempt = attempt;
        }

        public JobInstance(
            String name,
            TaskResult result)
            : this(name)
        {
            this.Result = result;
        }

        public JobInstance(Job job)
            : this(job, 1)
        {
        }

        public JobInstance(
            Job job,
            Int32 attempt)
            : this(job.Name, attempt)
        {
            this.Definition = job;
            this.State = PipelineState.NotStarted;
        }

        [DataMember]
        public String Identifier
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

        [DataMember]
        public Int32 Attempt
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? StartTime
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        [DataMember]
        public PipelineState State
        {
            get;
            set;
        }

        [DataMember]
        public TaskResult? Result
        {
            get;
            set;
        }

        [DataMember]
        public Job Definition
        {
            get;
            set;
        }

        public IDictionary<String, VariableValue> Outputs
        {
            get
            {
                if (m_outputs == null)
                {
                    m_outputs = new VariablesDictionary();
                }
                return m_outputs;
            }
        }

        [DataMember(Name = "Outputs")]
        private VariablesDictionary m_outputs;
    }
}
