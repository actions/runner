using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Variable : IVariable
    {
        public Variable()
        {
        }

        private Variable(Variable variableToClone)
        {
            this.Name = variableToClone.Name;
            this.Secret = variableToClone.Secret;
            this.Value = variableToClone.Value;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Secret
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Value
        {
            get;
            set;
        }

        public Variable Clone()
        {
            return new Variable(this);
        }

        VariableType IVariable.Type => VariableType.Inline;
    }
}
