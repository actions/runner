using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
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
            this.ContinueOnError = stepToClone.ContinueOnError;
            this.TimeoutInMinutes = stepToClone.TimeoutInMinutes;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Condition
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
        public Int32 TimeoutInMinutes
        {
            get;
            set;
        }
    }
}
