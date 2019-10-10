using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a phase target that runs on the server.
    /// </summary>
    [DataContract]
    public class ServerTarget : PhaseTarget
    {
        public ServerTarget()
            : base(PhaseTargetType.Server)
        {
        }

        internal ServerTarget(
            ISecuredObject securedObject)
            : base(PhaseTargetType.Server, securedObject)
        {
        }

        /// <summary>
        /// The execution options.
        /// </summary>
        [DataMember]
        public ServerTargetExecutionOptions ExecutionOptions
        {
            get;
            set;
        }
    }
}
