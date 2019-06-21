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
    public sealed class NumberContextData : PipelineContextData, INumber
    {
        public NumberContextData(Double value)
            : base(PipelineContextDataType.Number)
        {
            m_value = value;
        }

        public Double Value
        {
            get
            {
                return m_value;
            }
        }

        public override PipelineContextData Clone()
        {
            return new NumberContextData(m_value);
        }

        public override JToken ToJToken()
        {
            return (JToken)m_value;
        }

        public double GetDouble()
        {
            return Value;
        }

        public static implicit operator Double(NumberContextData data)
        {
            return data.Value;
        }

        public static implicit operator NumberContextData(Double data)
        {
            return new NumberContextData(data);
        }

        [DataMember(Name = "n", EmitDefaultValue = false)]
        private Double m_value;
    }
}
