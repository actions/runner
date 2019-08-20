using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [KnownType(typeof(ActionStep))]
    [KnownType(typeof(TaskStep))]
    [KnownType(typeof(TaskTemplateStep))]
    [KnownType(typeof(GroupStep))]
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
        public TemplateToken DisplayName
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
        Task = 1,

        [DataMember]
        TaskTemplate = 2,

        [DataMember]
        Group = 3,

        [DataMember]
        Action = 4,

        [DataMember]
        [Obsolete("Deprecated", false)]
        Script = 5,
    }
}
