using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the target for the build process.
    /// </summary>
    [DataContract]
    public class DesignerProcessTarget : BaseSecuredObject
    {
        public DesignerProcessTarget()
        {
        }

        public DesignerProcessTarget(ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// Agent specification for the build process.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentSpecification AgentSpecification { get; set; }
    }
}
