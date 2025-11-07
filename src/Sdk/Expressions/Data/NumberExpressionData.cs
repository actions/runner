using System;
using System.Globalization;
using System.Runtime.Serialization;
using GitHub.Actions.Expressions.Sdk;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Data
{
    [DataContract]
    public sealed class NumberExpressionData : ExpressionData, INumber
    {
        public NumberExpressionData(Double value)
            : base(ExpressionDataType.Number)
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

        public override ExpressionData Clone()
        {
            return new NumberExpressionData(m_value);
        }

        public override JToken ToJToken()
        {
            if (Double.IsNaN(m_value) || m_value == Double.PositiveInfinity || m_value == Double.NegativeInfinity)
            {
                return (JToken)m_value;
            }

            var floored = Math.Floor(m_value);
            if (m_value == floored && m_value <= (Double)Int32.MaxValue && m_value >= (Double)Int32.MinValue)
            {
                var flooredInt = (Int32)floored;
                return (JToken)flooredInt;
            }
            else if (m_value == floored && m_value <= (Double)Int64.MaxValue && m_value >= (Double)Int64.MinValue)
            {
                var flooredInt = (Int64)floored;
                return (JToken)flooredInt;
            }
            else
            {
                return (JToken)m_value;
            }
        }

        public override String ToString()
        {
            return m_value.ToString("G15", CultureInfo.InvariantCulture);
        }

        Double INumber.GetNumber()
        {
            return Value;
        }

        public static implicit operator Double(NumberExpressionData data)
        {
            return data.Value;
        }

        public static implicit operator NumberExpressionData(Double data)
        {
            return new NumberExpressionData(data);
        }

        [DataMember(Name = "n", EmitDefaultValue = false)]
        private Double m_value;
    }
}
