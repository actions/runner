using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the target for the docker build process.
    /// </summary>
    [DataContract]
    public class DockerProcessTarget: DesignerProcessTarget
    {
        public DockerProcessTarget()
        {
        }

        public DockerProcessTarget(ISecuredObject securedObject)
            : base(securedObject)
        {
        }
    }
}
