using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the target of a phase.
    /// </summary>
    [DataContract]
    [KnownType(typeof(AgentPoolQueueTarget))]
    [KnownType(typeof(ServerTarget))]
    [JsonConverter(typeof(PhaseTargetJsonConverter))]
    public abstract class PhaseTarget : BaseSecuredObject
    {
        protected PhaseTarget()
        {
        }

        internal protected PhaseTarget(
            Int32 type,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Type = type;
        }

        protected PhaseTarget(Int32 type)
        {
            this.Type = type;
        }

        /// <summary>
        /// The type of the target.
        /// </summary>
        /// <remarks>
        /// <see cref="PhaseTargetType" /> for valid phase target types.
        /// </remarks>
        [DataMember]
        public Int32 Type {
            get;
            private set;
        }
    }
}
