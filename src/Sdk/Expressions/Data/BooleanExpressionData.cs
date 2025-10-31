using System;
using System.Runtime.Serialization;
using GitHub.Actions.Expressions.Sdk;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Data
{
    [DataContract]
    public sealed class BooleanExpressionData : ExpressionData, IBoolean
    {
        public BooleanExpressionData(Boolean value)
            : base(ExpressionDataType.Boolean)
        {
            m_value = value;
        }

        public Boolean Value
        {
            get
            {
                return m_value;
            }
        }

        public override ExpressionData Clone()
        {
            return new BooleanExpressionData(m_value);
        }

        public override JToken ToJToken()
        {
            return (JToken)m_value;
        }

        public override String ToString()
        {
            return m_value ? "true" : "false";
        }

        Boolean IBoolean.GetBoolean()
        {
            return Value;
        }

        public static implicit operator Boolean(BooleanExpressionData data)
        {
            return data.Value;
        }

        public static implicit operator BooleanExpressionData(Boolean data)
        {
            return new BooleanExpressionData(data);
        }

        [DataMember(Name = "b", EmitDefaultValue = false)]
        private Boolean m_value;
    }
}
