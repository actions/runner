using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class StringContextData : PipelineContextData, IString
    {
        public StringContextData(String value)
            : base(PipelineContextDataType.String)
        {
            m_value = value;
        }

        public String Value
        {
            get
            {
                if (m_value == null)
                {
                    m_value = String.Empty;
                }

                return m_value;
            }
        }

        public override PipelineContextData Clone()
        {
            return new StringContextData(m_value);
        }

        public override JToken ToJToken()
        {
            return (JToken)m_value;
        }

        String IString.GetString()
        {
            return Value;
        }

        public override String ToString()
        {
            return Value;
        }

        public static implicit operator String(StringContextData data)
        {
            return data.Value;
        }

        public static implicit operator StringContextData(String data)
        {
            return new StringContextData(data);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_value?.Length == 0)
            {
                m_value = null;
            }
        }

        [DataMember(Name = "s", EmitDefaultValue = false)]
        private String m_value;
    }
}
