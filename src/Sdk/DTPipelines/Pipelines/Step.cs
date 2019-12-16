using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [KnownType(typeof(ActionStep))]
    [JsonConverter(typeof(StepConverter))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Step
    {
        protected Step()
        {
            this.Enabled = true;
        }

        protected Step(Step stepToClone)
        {
            this.Enabled = stepToClone.Enabled;
            this.Id = stepToClone.Id;
            this.Name = stepToClone.Name;
            this.DisplayName = stepToClone.DisplayName;
        }

        [DataMember(EmitDefaultValue = false)]
        public abstract StepType Type
        {
            get;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
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

        [DefaultValue(true)]
        [DataMember(EmitDefaultValue = false)]
        public Boolean Enabled
        {
            get;
            set;
        }

        public abstract Step Clone();
    }

    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum StepType
    {
        [DataMember]
        Action = 4,
    }
}
