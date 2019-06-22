using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents options for running a phase against multiple agents.
    /// </summary>
    [DataContract]
    public class MultipleAgentExecutionOptions : AgentTargetExecutionOptions
    {
        public MultipleAgentExecutionOptions()
            : this(null)
        {
        }

        internal MultipleAgentExecutionOptions(
            ISecuredObject securedObject)
            : base(AgentTargetExecutionType.MultipleAgents, securedObject)
        {
            MaxConcurrency = 1;
        }

        /// <summary>
        /// The maximum number of agents to use simultaneously.
        /// </summary>
        [DataMember(EmitDefaultValue=true)]
        [DefaultValue(1)]
        public Int32 MaxConcurrency {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether failure on one agent should prevent the phase from running on other agents.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean ContinueOnError
        {
            get;
            set;
        }
    }
}
