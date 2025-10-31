#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Actions.Expressions.Data;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class StrategyConfiguration
    {
        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public String Name { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public String Id { get; set; }

        [IgnoreDataMember]
        public Dictionary<String, ExpressionData> ExpressionData
        {
            get
            {
                if (m_expressionData is null)
                {
                    m_expressionData = new Dictionary<String, ExpressionData>(StringComparer.Ordinal);
                }
                return m_expressionData;
            }
        }

        [DataMember(Name = "expressionData", EmitDefaultValue = false)]
        private Dictionary<String, ExpressionData> m_expressionData;
    }
}
