﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json.Linq;

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

        public override String ToString()
        {
            return m_value.ToString("G17", CultureInfo.InvariantCulture);
        }

        Double INumber.GetNumber()
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
