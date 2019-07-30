using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class JobStep : Step
    {
        [JsonConstructor]
        public JobStep()
        {
            this.Enabled = true;
        }

        protected JobStep(JobStep stepToClone)
            : base(stepToClone)
        {
            this.Condition = stepToClone.Condition;
            this.ContinueOnError = stepToClone.ContinueOnError?.Clone();
            this.TimeoutInMinutes = stepToClone.TimeoutInMinutes?.Clone();
        }

        [DataMember(EmitDefaultValue = false)]
        public String Condition
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken ContinueOnError
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken TimeoutInMinutes
        {
            get;
            set;
        }
    }
}
