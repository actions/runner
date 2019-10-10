using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents options for running a phase on the server.
    /// </summary>
    [DataContract]
    [KnownType(typeof(VariableMultipliersServerExecutionOptions))]
    [JsonConverter(typeof(ServerTargetExecutionOptionsJsonConverter))]
    public class ServerTargetExecutionOptions : BaseSecuredObject
    {
        public ServerTargetExecutionOptions()
            : this(ServerTargetExecutionType.Normal)
        {
        }

        protected ServerTargetExecutionOptions(Int32 type)
            : this(type, null)
        {
        }

        internal ServerTargetExecutionOptions(
            ISecuredObject securedObject)
            : this(ServerTargetExecutionType.Normal, securedObject)
        {
        }

        internal ServerTargetExecutionOptions(
            Int32 type,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Type = type;
        }

        /// <summary>
        /// The type.
        /// </summary>
        /// <remarks>
        /// <see cref="ServerTargetExecutionType" /> for supported types.
        /// </remarks>
        [DataMember(EmitDefaultValue = true)]
        public Int32 Type
        {
            get;
            private set;
        }
    }
}
