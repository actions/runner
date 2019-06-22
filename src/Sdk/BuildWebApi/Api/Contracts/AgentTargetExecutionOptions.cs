using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Additional options for running phases against an agent queue.
    /// </summary>
    [DataContract]
    [KnownType(typeof(MultipleAgentExecutionOptions))]
    [KnownType(typeof(VariableMultipliersAgentExecutionOptions))]
    [JsonConverter(typeof(AgentTargetExecutionOptionsJsonConverter))]
    public class AgentTargetExecutionOptions : BaseSecuredObject
    {
        public AgentTargetExecutionOptions()
            : this(AgentTargetExecutionType.Normal)
        {
        }

        protected AgentTargetExecutionOptions(Int32 type)
            : this(type, null)
        {
        }

        internal AgentTargetExecutionOptions(
            ISecuredObject securedObject)
            : this(AgentTargetExecutionType.Normal, securedObject)
        {
        }

        internal AgentTargetExecutionOptions(
            Int32 type,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Type = type;
        }

        /// <summary>
        /// Indicates the type of execution options.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Int32 Type
        {
            get;
            set;
        }
    }
}
