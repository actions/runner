using GitHub.DistributedTask.Expressions;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class NumberContextData : PipelineContextData, INumber
    {
        public NumberContextData(Decimal value)
            : base(PipelineContextDataType.Number)
        {
            m_value = value;
        }

        public Decimal Value
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

        public override String ToString()
        {
            return m_value.ToString("0.#######", CultureInfo.InvariantCulture);
        }

        public Decimal GetNumber()
        {
            return Value;
        }

        public static implicit operator Decimal(NumberContextData data)
        {
            return data.Value;
        }

        public static implicit operator NumberContextData(Decimal data)
        {
            return new NumberContextData(data);
        }

        [DataMember(Name = "n", EmitDefaultValue = false)]
        private Decimal m_value;
    }
}
