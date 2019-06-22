using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Specification of the agent defined by the pool provider.
    /// </summary>
    [DataContract]
    public class AgentSpecification: BaseSecuredObject
    {
        public AgentSpecification()
        {
        }

        public AgentSpecification(ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// Agent specification unique identifier.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Identifier { get; set; }
    }
}
