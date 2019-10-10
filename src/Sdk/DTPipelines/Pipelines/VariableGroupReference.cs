using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VariableGroupReference : ResourceReference, IVariable
    {
        public VariableGroupReference()
        {
        }

        private VariableGroupReference(VariableGroupReference referenceToCopy)
            : base(referenceToCopy)
        {
            this.Id = referenceToCopy.Id;
            this.GroupType = referenceToCopy.GroupType;
            this.SecretStore = referenceToCopy.SecretStore?.Clone();
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String GroupType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public SecretStoreConfiguration SecretStore
        {
            get;
            set;
        }

        public VariableGroupReference Clone()
        {
            return new VariableGroupReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString();
        }

        [DataMember(Name = nameof(Type))]
        VariableType IVariable.Type
        {
            get
            {
                return VariableType.Group;
            }
        }
    }
}
