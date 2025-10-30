using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Data
{
    /// <summary>
    /// Base class for all template tokens
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(ExpressionDataJsonConverter))]
    public abstract class ExpressionData
    {
        protected ExpressionData(Int32 type)
        {
            Type = type;
        }

        [DataMember(Name = "t", EmitDefaultValue = false)]
        internal Int32 Type { get; }

        public abstract ExpressionData Clone();

        public abstract JToken ToJToken();
    }
}