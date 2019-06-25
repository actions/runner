using GitHub.DistributedTask.Expressions;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class BooleanContextData : PipelineContextData, IBoolean
    {
        public BooleanContextData(Boolean value)
            : base(PipelineContextDataType.Boolean)
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

        public override PipelineContextData Clone()
        {
            return new BooleanContextData(m_value);
        }

        public override JToken ToJToken()
        {
            return (JToken)m_value;
        }

        public override String ToString()
        {
            return m_value.ToString().ToLower();
        }

        public bool GetBoolean()
        {
            return Value;
        }

        public static implicit operator Boolean(BooleanContextData data)
        {
            return data.Value;
        }

        public static implicit operator BooleanContextData(Boolean data)
        {
            return new BooleanContextData(data);
        }

        [DataMember(Name = "b", EmitDefaultValue = false)]
        private Boolean m_value;
    }
}
